using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Meziantou.Backup.FileSystem.Abstractions;
using Meziantou.Backup.FileSystem.Aes;
using Microsoft.Extensions.CommandLineUtils;

namespace Meziantou.Backup.Console
{
    internal class BackupConsole
    {
        private string GetValue(CommandOption option, string defaultValue)
        {
            if (!option.HasValue())
                return defaultValue;

            return option.Value();
        }

        private int GetValue(CommandOption option, int defaultValue)
        {
            if (!option.HasValue())
                return defaultValue;

            int v;
            if (int.TryParse(option.Value(), out v))
                return v;

            return defaultValue;
        }

        private bool GetValue(CommandOption option, bool defaultValue)
        {
            if (!option.HasValue())
                return defaultValue;

            if (option.OptionType == CommandOptionType.NoValue)
                return true;

            bool v;
            if (bool.TryParse(option.Value(), out v))
                return v;

            return defaultValue;
        }

        private T GetValue<T>(CommandOption option, T defaultValue) where T : struct
        {
            if (!option.HasValue())
                return defaultValue;

            if (typeof(T).IsEnum)
            {
                T v;
                if (Enum.TryParse(option.Value(), true, out v))
                    return v;
            }

            return defaultValue;
        }

        private T? GetValue<T>(CommandOption option, T? defaultValue) where T : struct
        {
            if (!option.HasValue())
                return defaultValue;

            if (typeof(T).IsEnum)
            {
                T v;
                if (Enum.TryParse(option.Value(), true, out v))
                    return v;
            }

            return defaultValue;
        }

        public void Main(string[] args)
        {

            var app = new CommandLineApplication();
            app.HelpOption();

            app.Command("backup", throwOnUnexpectedArg: false, configuration: command =>
            {
                command.HelpOption();

                var sourceArgument = command.Argument("[SOURCE]", "");
                var targetArgument = command.Argument("[TARGET]", "");

                var sourceOptions = command.Option("--source <PATH>", "", CommandOptionType.SingleValue);
                var targetOptions = command.Option("--target <PATH>", "", CommandOptionType.SingleValue);

                var sourceConfigurationOptions = command.Option("-sc|--sourceConfiguration <OPTIONS>", "", CommandOptionType.MultipleValue);
                var targetConfigurationOptions = command.Option("-tc|--targetConfiguration <OPTIONS>", "", CommandOptionType.MultipleValue);

                var equalityOptions = command.Option("--equality <EqualityMethods>", "", CommandOptionType.SingleValue);
                var retryCountOptions = command.Option("--retry <RetryCount>", "", CommandOptionType.SingleValue);
                var createDirectoriesOptions = command.Option("--createDirectories <CanCreateDirectories>", "", CommandOptionType.SingleValue);
                var deleteDirectoriesOptions = command.Option("--deleteDirectories <CanDeleteDirectories>", "", CommandOptionType.SingleValue);
                var createFilesOptions = command.Option("--createFiles <CanCreateFiles>", "", CommandOptionType.SingleValue);
                var updateFilesOptions = command.Option("--updateFiles <CanUpdateFiles>", "", CommandOptionType.SingleValue);
                var deleteFilesOptions = command.Option("--deleteFiles <CanDeleteFiles>", "", CommandOptionType.SingleValue);
                var continueOnErrorOptions = command.Option("--ignoreErrors", "", CommandOptionType.NoValue);

                var sourceAesMethod = command.Option("--sourceAesMethod <METHOD>", "", CommandOptionType.SingleValue);
                var sourceAesPassword = command.Option("--sourceAesPassword <PASSWORD>", "", CommandOptionType.SingleValue);
                var sourceAesEncryptFileNames = command.Option("--sourceAesEncryptFileNames", "", CommandOptionType.NoValue);
                var sourceAesEncryptDirectoryNames = command.Option("--sourceAesEncryptDirectoryNames", "", CommandOptionType.NoValue);

                var targetAesMethod = command.Option("--targetAesMethod <METHOD>", "", CommandOptionType.SingleValue);
                var targetAesPassword = command.Option("--targetAesPassword <PASSWORD>", "", CommandOptionType.SingleValue);
                var targetAesEncryptFileNames = command.Option("--targetAesEncryptFileNames", "", CommandOptionType.NoValue);
                var targetAesEncryptDirectoryNames = command.Option("--targetAesEncryptDirectoryNames", "", CommandOptionType.NoValue);

                command.OnExecute(() =>
                {
                    var backup = new Backup();
                    backup.Action += Backup_Action;
                    backup.Error += Backup_Error;
                    backup.Copying += Backup_Copying;

                    backup.EqualityMethods = GetValue(equalityOptions, FileInfoEqualityMethods.Default);
                    backup.RetryCount = GetValue(retryCountOptions, 3);
                    backup.CanCreateDirectories = GetValue(createDirectoriesOptions, true);
                    backup.CanDeleteDirectories = GetValue(deleteDirectoriesOptions, false);
                    backup.CanCreateFiles = GetValue(createFilesOptions, true);
                    backup.CanUpdateFiles = GetValue(updateFilesOptions, true);
                    backup.CanDeleteFiles = GetValue(deleteFilesOptions, false);
                    backup.ContinueOnError = GetValue(continueOnErrorOptions, false);

                    var summary = new BackupSummary(backup);
                    try
                    {
                        string sourcePath;
                        string targetPath;
                        var sourceFileSystem = GetFileSystem(new object[] { sourceArgument, sourceOptions }, sourceConfigurationOptions, out sourcePath);
                        if (sourceFileSystem == null)
                            return 1;

                        var targetFileSystem = GetFileSystem(new object[] { targetArgument, targetOptions }, targetConfigurationOptions, out targetPath);
                        if (targetFileSystem == null)
                            return 2;

                        sourceFileSystem = CreateAesFileSystem(sourceFileSystem, sourceAesMethod, sourceAesPassword, sourceAesEncryptFileNames, sourceAesEncryptDirectoryNames);
                        targetFileSystem = CreateAesFileSystem(targetFileSystem, targetAesMethod, targetAesPassword, targetAesEncryptFileNames, targetAesEncryptDirectoryNames);

                        using (var cts = new CancellationTokenSource())
                        {
                            System.Console.CancelKeyPress += (sender, eventArgs) =>
                            {
                                cts.Cancel();
                                eventArgs.Cancel = true;
                            };

                            var backupAsync = RunAsync(backup, sourceFileSystem, sourcePath, targetFileSystem, targetPath, cts.Token);
                            var awaiter = backupAsync.GetAwaiter();
                            awaiter.GetResult();
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        System.Console.WriteLine("Operation was canceled");
                    }

                    System.Console.WriteLine("Directories: " + summary.DirectoryCount);
                    System.Console.WriteLine("  Created: " + summary.DirectoryCreatedCount);
                    System.Console.WriteLine("  Deleted: " + summary.DirectoryDeletedCount);

                    System.Console.WriteLine("File: " + summary.FileCount);
                    System.Console.WriteLine("  Created: " + summary.FileCreatedCount);
                    System.Console.WriteLine("  Updated: " + summary.FileUpdatedCount);
                    System.Console.WriteLine("  Deleted: " + summary.FileDeletedCount);

                    System.Console.WriteLine("Errors: " + summary.ErrorCount);

                    return 0;
                });
            });

            app.Execute(args);
        }

        private IFileSystem CreateAesFileSystem(IFileSystem fileSystem, CommandOption method, CommandOption password, CommandOption encryptFileNames, CommandOption encryptDirectoryNames)
        {
            if (!password.HasValue())
                return fileSystem;

            var aesFs = new AesFileSystem(fileSystem);
            aesFs.Version = GetValue(method, AesVersion.Aes256);
            aesFs.Password = GetValue(password, (string)null);
            aesFs.EncryptDirectoryName = GetValue(encryptDirectoryNames, false);
            aesFs.EncryptFileName = GetValue(encryptFileNames, false);

            return aesFs;
        }

        private IFileSystem GetFileSystem(IEnumerable<object> pathOptions, CommandOption configuration, out string path)
        {
            path = null;
            var fullPath = GetPath(pathOptions);
            if (fullPath == null)
                return null;

            string providerName;
            var index = fullPath.IndexOf("://", StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                providerName = null;
                path = fullPath;
            }
            else
            {
                providerName = fullPath.Substring(0, index);
                path = fullPath.Substring(index + 3);
            }

            var providerType = GetType(providerName);
            if (providerType == null)
                return null;

            var fileSystemProvider = CreateInstance(providerType) as IFileSystem;
            if (fileSystemProvider == null)
                return null;

            var configDictionary = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            if (configuration.Values != null)
            {
                foreach (var value in configuration.Values)
                {
                    var indexOfEqual = value.IndexOf('=');
                    if (indexOfEqual < 0)
                        continue;

                    configDictionary.Add(value.Substring(0, indexOfEqual), value.Substring(indexOfEqual + 1));
                }
            }

            fileSystemProvider.Initialize(configDictionary);

            return fileSystemProvider;
        }

        private string GetPath(IEnumerable<object> pathOptions)
        {
            foreach (var item in pathOptions)
            {
                var option = item as CommandOption;
                if (option != null)
                {
                    if (option.HasValue())
                    {
                        foreach (var value in option.Values)
                        {
                            if (!string.IsNullOrEmpty(value))
                                return value;
                        }
                    }
                }
                else
                {
                    var argument = item as CommandArgument;
                    if (argument?.Values != null)
                    {
                        foreach (var value in argument.Values)
                        {
                            if (!string.IsNullOrEmpty(value))
                                return value;
                        }
                    }
                }
            }

            return null;
        }

        private async Task RunAsync(Backup backup, IFileSystem sourceFileSystem, string sourcePath, IFileSystem targetFileSystem, string targetPath, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var sourceDirectory = await GetOrCreateRootDirectoryItemAsync(sourceFileSystem, sourcePath, ct);
                var targetDirectory = await GetOrCreateRootDirectoryItemAsync(targetFileSystem, targetPath, ct);

                await backup.RunAsync(sourceDirectory, targetDirectory, ct).ConfigureAwait(false);
            }
            finally
            {
                var sourceDisposable = sourceFileSystem as IDisposable;
                sourceDisposable?.Dispose();

                var targetDisposable = targetFileSystem as IDisposable;
                targetDisposable?.Dispose();
            }
        }

        protected virtual object CreateInstance(Type type)
        {
            return Activator.CreateInstance(type);
        }

        protected virtual Type GetType(string name)
        {
            if (string.IsNullOrEmpty(name) ||
                string.Equals("file", name, StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals("FileSystem", name, StringComparison.InvariantCultureIgnoreCase))
                return Type.GetType("Meziantou.Backup.FileSystem.Physical.PhysicalFileSystem, Meziantou.Backup.FileSystem.Physical");

            if (string.Equals("OneDrive", name, StringComparison.InvariantCultureIgnoreCase))
                return Type.GetType("Meziantou.Backup.FileSystem.OneDrive.OneDriveFileSystem, Meziantou.Backup.FileSystem.OneDrive");

            if (string.Equals("AES", name, StringComparison.InvariantCultureIgnoreCase))
                return Type.GetType("Meziantou.Backup.FileSystem.Aes.AesFileSystem, Meziantou.Backup.FileSystem.Aes");

            if (string.Equals("sftp", name, StringComparison.InvariantCultureIgnoreCase))
                return Type.GetType("Meziantou.Backup.FileSystem.Sftp.SftpFileSystem, Meziantou.Backup.FileSystem.Sftp");

            return Type.GetType(name, true);
        }

        private async Task<IDirectoryInfo> GetOrCreateRootDirectoryItemAsync(IFileSystem fileSystem, string path, CancellationToken ct)
        {
            var authenticable = fileSystem as IAuthenticable;
            if (authenticable != null)
            {
                await authenticable.LogInAsync(ct).ConfigureAwait(false);
            }

            return await fileSystem.GetOrCreateDirectoryItemAsync(path, ct).ConfigureAwait(false);
        }

        private void Backup_Copying(object sender, FileCopyingEventArgs e)
        {
            float percent = ((float)e.CurrentPosition) / e.FileLength * 100f;
            System.Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId}] Copying ({percent:F1}% - {FriendlyFileLength(e.CurrentPosition)}/{FriendlyFileLength(e.FileLength)}): <{GetDisplayName(e.SourceItem)}> -> <{GetDisplayName(e.TargetItem)}>");
        }

        private string FriendlyFileLength(long length)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
            int s = 0;

            while (length >= 1024)
            {
                s++;
                length /= 1024;
            }

            return $"{length} {suffixes[s]}";
        }

        private void Backup_Error(object sender, BackupErrorEventArgs e)
        {
            if (e.Exception is OperationCanceledException)
            {
                System.Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId}] Error: Operation canceled");
                e.Cancel = true;
            }
            else
            {
                System.Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId}] Retry ({e.RetryCount}): {e.Exception}");
            }
        }

        private void Backup_Action(object sender, BackupActionEventArgs e)
        {
            // Filter some actions...
            if (e.Action == BackupAction.Synchronized ||
                e.Action == BackupAction.Created ||
                e.Action == BackupAction.Updated ||
                e.Action == BackupAction.Deleted)
                return;

            if (e.Action == BackupAction.Synchronizing && e.SourceItem.IsFile())
                return;

            if (e.Method != FileInfoEqualityMethods.None)
            {
                if (e.SourceItem.IsFile() && e.Method == FileInfoEqualityMethods.Length)
                {
                    System.Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId}] {e.Action} ({e.Method}: {((IFileInfo)e.SourceItem).Length}-{((IFileInfo)e.TargetItem).Length}): \"{GetDisplayName(e.SourceItem)}\" -> \"{GetDisplayName(e.TargetItem)}\"");
                }
                else
                {
                    System.Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId}] {e.Action} ({e.Method}): \"{GetDisplayName(e.SourceItem)}\" -> \"{GetDisplayName(e.TargetItem)}\"");
                }
            }
            else
            {
                System.Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId}] {e.Action}: \"{GetDisplayName(e.SourceItem)}\" -> \"{GetDisplayName(e.TargetItem)}\"");
            }
        }

        private string GetDisplayName(IFileSystemInfo item)
        {
            var fullName = item as IFullName;
            if (fullName != null)
                return fullName.FullName;

            return item?.Name;
        }
    }
}