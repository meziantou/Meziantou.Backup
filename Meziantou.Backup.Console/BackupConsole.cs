using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Meziantou.Backup.FileSystem.Abstractions;
using Meziantou.Backup.FileSystem.Aes;
using Meziantou.Framework.Security;
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

                var keepHistoryOption = command.Option("--keepHistory", "", CommandOptionType.NoValue);
                var equalityOption = command.Option("--equality <EqualityMethods>", "", CommandOptionType.SingleValue);
                var retryCountOption = command.Option("--retry <RetryCount>", "", CommandOptionType.SingleValue);
                var createDirectoryOption = command.Option("--createDirectories <CanCreateDirectories>", "", CommandOptionType.SingleValue);
                var deleteDirectoriesOption = command.Option("--deleteDirectories <CanDeleteDirectories>", "", CommandOptionType.SingleValue);
                var createFilesOption = command.Option("--createFiles <CanCreateFiles>", "", CommandOptionType.SingleValue);
                var updateFilesOption = command.Option("--updateFiles <CanUpdateFiles>", "", CommandOptionType.SingleValue);
                var deleteFilesOption = command.Option("--deleteFiles <CanDeleteFiles>", "", CommandOptionType.SingleValue);
                var continueOnErrorOption = command.Option("--ignoreErrors", "", CommandOptionType.NoValue);

                var sourceAesMethodOption = command.Option("--sourceAesMethod <METHOD>", "", CommandOptionType.SingleValue);
                var sourceAesPasswordOption = command.Option("--sourceAesPassword <PASSWORD>", "", CommandOptionType.SingleValue);
                var sourceAesPasswordNameOption = command.Option("--sourceAesPasswordName <NAME>", "", CommandOptionType.SingleValue);
                var sourceAesAskPasswordOption = command.Option("--sourceAesAskPassword", "", CommandOptionType.NoValue);
                var sourceAesEncryptFileNamesOption = command.Option("--sourceAesEncryptFileNames", "", CommandOptionType.NoValue);
                var sourceAesEncryptDirectoryNamesOption = command.Option("--sourceAesEncryptDirectoryNames", "", CommandOptionType.NoValue);

                var targetAesMethodOption = command.Option("--targetAesMethod <METHOD>", "", CommandOptionType.SingleValue);
                var targetAesPasswordOption = command.Option("--targetAesPassword <PASSWORD>", "", CommandOptionType.SingleValue);
                var targetAesPasswordNameOption = command.Option("--targetAesPasswordName <NAME>", "", CommandOptionType.SingleValue);
                var targetAesAskPasswordOption = command.Option("--targetAesAskPassword", "", CommandOptionType.NoValue);
                var targetAesEncryptFileNamesOption = command.Option("--targetAesEncryptFileNames", "", CommandOptionType.NoValue);
                var targetAesEncryptDirectoryNamesOption = command.Option("--targetAesEncryptDirectoryNames", "", CommandOptionType.NoValue);

                var testConfig = command.Option("--test-config", "for testing purpose only", CommandOptionType.NoValue);

                command.OnExecute(() =>
                {
                    var backup = new Backup();
                    backup.Action += Backup_Action;
                    backup.Error += Backup_Error;
                    backup.Copying += Backup_Copying;

                    backup.EqualityMethods = GetValue(equalityOption, FileInfoEqualityMethods.Default);
                    backup.RetryCount = GetValue(retryCountOption, 3);
                    backup.CanCreateDirectories = GetValue(createDirectoryOption, true);
                    backup.CanDeleteDirectories = GetValue(deleteDirectoriesOption, false);
                    backup.CanCreateFiles = GetValue(createFilesOption, true);
                    backup.CanUpdateFiles = GetValue(updateFilesOption, true);
                    backup.CanDeleteFiles = GetValue(deleteFilesOption, false);
                    backup.ContinueOnError = GetValue(continueOnErrorOption, false);
                    backup.KeepHistory = GetValue(keepHistoryOption, false);

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

                        sourceFileSystem = CreateAesFileSystem(sourceFileSystem, sourceAesMethodOption, sourceAesPasswordOption, sourceAesPasswordNameOption, sourceAesAskPasswordOption, sourceAesEncryptFileNamesOption, sourceAesEncryptDirectoryNamesOption);
                        if (sourceFileSystem == null)
                            return 3;

                        targetFileSystem = CreateAesFileSystem(targetFileSystem, targetAesMethodOption, targetAesPasswordOption, targetAesPasswordNameOption, targetAesAskPasswordOption, targetAesEncryptFileNamesOption, targetAesEncryptDirectoryNamesOption);
                        if (targetFileSystem == null)
                            return 4;

                        using (var cts = new CancellationTokenSource())
                        {
                            System.Console.CancelKeyPress += (sender, eventArgs) =>
                            {
                                cts.Cancel();
                                eventArgs.Cancel = true;
                            };

                            if (!GetValue(testConfig, false))
                            {
                                var backupAsync = RunAsync(backup, sourceFileSystem, sourcePath, targetFileSystem, targetPath, cts.Token);
                                var awaiter = backupAsync.GetAwaiter();
                                awaiter.GetResult();
                            }
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

        private IFileSystem CreateAesFileSystem(IFileSystem fileSystem, CommandOption method, CommandOption password, CommandOption passwordName, CommandOption askPassword, CommandOption encryptFileNames, CommandOption encryptDirectoryNames)
        {
            if (!password.HasValue() && !askPassword.HasValue() && !passwordName.HasValue())
                return fileSystem;

            string pwd = GetValue(password, (string)null);
            if (string.IsNullOrEmpty(pwd))
            {
                var applicationName = GetValue(passwordName, null);
                if (!string.IsNullOrEmpty(applicationName))
                {
                    var cred = CredentialManager.ReadCredential(applicationName);
                    if (cred != null)
                    {
                        pwd = cred.Password;
                    }
                }
            }

            if (string.IsNullOrEmpty(pwd) && GetValue(askPassword, false))
            {
                System.Console.Write("Enter password: ");
                pwd = GetPassword();
                System.Console.WriteLine();
            }

            if (string.IsNullOrEmpty(pwd))
                return null;

            var aesFs = new AesFileSystem(fileSystem);
            aesFs.Version = GetValue(method, AesVersion.Aes256);
            aesFs.EncryptDirectoryName = GetValue(encryptDirectoryNames, false);
            aesFs.EncryptFileName = GetValue(encryptFileNames, false);
            aesFs.Password = pwd;
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

        public string GetPassword()
        {
            var pwd = new StringBuilder();
            while (true)
            {
                ConsoleKeyInfo i = System.Console.ReadKey(true);
                if (i.Key == ConsoleKey.Enter)
                {
                    break;
                }
                else if (i.Key == ConsoleKey.Backspace)
                {
                    if (pwd.Length > 0)
                    {
                        pwd.Remove(pwd.Length - 1, 1);
                        System.Console.Write("\b \b");
                    }
                }
                else
                {
                    pwd.Append(i.KeyChar);
                    System.Console.Write("*");
                }
            }

            return pwd.ToString();
        }
    }
}