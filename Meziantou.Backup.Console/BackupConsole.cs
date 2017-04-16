using System;
using System.Collections.Generic;
using System.IO;
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

            if (int.TryParse(option.Value(), out int v))
                return v;

            return defaultValue;
        }

        private bool GetValue(CommandOption option, bool defaultValue)
        {
            if (!option.HasValue())
                return defaultValue;

            if (option.OptionType == CommandOptionType.NoValue)
                return true;

            if (bool.TryParse(option.Value(), out bool v))
                return v;

            return defaultValue;
        }

        private T GetValue<T>(CommandOption option, T defaultValue) where T : struct
        {
            if (!option.HasValue())
                return defaultValue;

            if (typeof(T).IsEnum)
            {
                if (Enum.TryParse(option.Value(), true, out T v))
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
                if (Enum.TryParse(option.Value(), true, out T v))
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

                var keepHistoryOption = command.Option("--keepHistory", "Preserve all versions of a file in the target file system (sufix the file name with the utc date)", CommandOptionType.NoValue);
                var readHistoryOption = command.Option("--readHistory", "", CommandOptionType.NoValue);
                var writeHistoryOption = command.Option("--writeHistory", "", CommandOptionType.NoValue);
                var equalityOption = command.Option("--equality <EqualityMethods>", "Length, LastWriteTime, Content, ContentMd5, ContentSha1, ContentSha256, ContentSha512", CommandOptionType.SingleValue);
                var createDirectoryOption = command.Option("--createDirectories <TRUE,FALSE>", "Can create directories in the target file system", CommandOptionType.SingleValue);
                var deleteDirectoriesOption = command.Option("--deleteDirectories <TRUE,FALSE>", "Can delete directories in the target file system", CommandOptionType.SingleValue);
                var createFilesOption = command.Option("--createFiles <TRUE,FALSE>", "Can create files in the target file system", CommandOptionType.SingleValue);
                var updateFilesOption = command.Option("--updateFiles <TRUE,FALSE>", "Can update files in the target file system", CommandOptionType.SingleValue);
                var deleteFilesOption = command.Option("--deleteFiles <TRUE,FALSE>", "Can delete files in the target file system", CommandOptionType.SingleValue);
                var retryCountOption = command.Option("--retry <RetryCount>", "Number of attempts of an operation", CommandOptionType.SingleValue);
                var continueOnErrorOption = command.Option("--ignoreErrors", "continue on error", CommandOptionType.NoValue);
                var diffFilesPathOption = command.Option("--diffFilesPath", "", CommandOptionType.SingleValue);

                var sourceAesMethodOption = command.Option("--sourceAesMethod <METHOD>", "AES128, AES256", CommandOptionType.SingleValue);
                var sourceAesPasswordOption = command.Option("--sourceAesPassword <PASSWORD>", "Password to decrypt files", CommandOptionType.SingleValue);
                var sourceAesPasswordNameOption = command.Option("--sourceAesPasswordName <NAME>", "Name of the application in the Windows Credential Manager", CommandOptionType.SingleValue);
                var sourceAesAskPasswordOption = command.Option("--sourceAesAskPassword", "Ask for password", CommandOptionType.NoValue);
                var sourceAesEncryptFileNamesOption = command.Option("--sourceAesEncryptFileNames", "Decrypt file names", CommandOptionType.NoValue);
                var sourceAesEncryptDirectoryNamesOption = command.Option("--sourceAesEncryptDirectoryNames", "Decrypt directory names", CommandOptionType.NoValue);

                var targetAesMethodOption = command.Option("--targetAesMethod <METHOD>", "AES128, AES256", CommandOptionType.SingleValue);
                var targetAesPasswordOption = command.Option("--targetAesPassword <PASSWORD>", "Password to encrypt files", CommandOptionType.SingleValue);
                var targetAesPasswordNameOption = command.Option("--targetAesPasswordName <NAME>", "Name of the application in the Windows Credential Manager", CommandOptionType.SingleValue);
                var targetAesAskPasswordOption = command.Option("--targetAesAskPassword", "Ask for password", CommandOptionType.NoValue);
                var targetAesEncryptFileNamesOption = command.Option("--targetAesEncryptFileNames", "Encrypt file names", CommandOptionType.NoValue);
                var targetAesEncryptDirectoryNamesOption = command.Option("--targetAesEncryptDirectoryNames", "Encrypt directory names", CommandOptionType.NoValue);

                var logLevelOption = command.Option("--log", "", CommandOptionType.SingleValue);
                var testConfig = command.Option("--test-config", "for testing purpose only", CommandOptionType.NoValue);

                command.OnExecute(() =>
                {
                    var backup = new Backup();
                    var backupConsole = new ConsoleLogger(backup);
                    backupConsole.Level = GetValue(logLevelOption, ConsoleLoggerLevel.Default);

                    backup.EqualityMethods = GetValue(equalityOption, FileInfoEqualityMethods.Default);
                    backup.RetryCount = GetValue(retryCountOption, 3);
                    backup.CanCreateDirectories = GetValue(createDirectoryOption, true);
                    backup.CanDeleteDirectories = GetValue(deleteDirectoriesOption, false);
                    backup.CanCreateFiles = GetValue(createFilesOption, true);
                    backup.CanUpdateFiles = GetValue(updateFilesOption, true);
                    backup.CanDeleteFiles = GetValue(deleteFilesOption, false);
                    backup.ContinueOnError = GetValue(continueOnErrorOption, false);
                    backup.ReadHistory = GetValue(readHistoryOption, GetValue(keepHistoryOption, false));
                    backup.WriteHistory = GetValue(writeHistoryOption, GetValue(keepHistoryOption, false));

                    string writeDiffFilesPath = GetValue(diffFilesPathOption, null);
                    if (writeDiffFilesPath != null)
                    {
                        backup.Action += (sender, e) =>
                        {
                            if (e.Action == BackupAction.Creating || e.Action == BackupAction.Deleting)
                            {
                                if (e.SourceItem is IFileInfo fi)
                                {
                                    e.Cancel = true;
                                    var fullPath = Path.Combine(writeDiffFilesPath, string.Join(Path.DirectorySeparatorChar.ToString(), e.Path));
                                    Directory.CreateDirectory(fullPath);
                                    using (var stream = fi.OpenReadAsync(CancellationToken.None).Result)
                                    using (var streamWriter = File.Create(Path.Combine(fullPath, e.SourceItem.Name)))
                                    {
                                        stream.CopyTo(streamWriter);
                                    }
                                }
                            }
                        };
                    }

                    var summary = new BackupSummary(backup);
                    try
                    {
                        var sourceFileSystem = GetFileSystem(new object[] { sourceArgument, sourceOptions }, sourceConfigurationOptions, out string sourcePath);
                        if (sourceFileSystem == null)
                            return 1;

                        var targetFileSystem = GetFileSystem(new object[] { targetArgument, targetOptions }, targetConfigurationOptions, out string targetPath);
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

            string applicationName = null;
            string pwd = GetValue(password, (string)null);
            if (string.IsNullOrEmpty(pwd))
            {
                applicationName = GetValue(passwordName, null);
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

                // Save password
                if (!string.IsNullOrEmpty(applicationName) && !string.IsNullOrEmpty(pwd))
                {
                    CredentialManager.WriteCredential(applicationName, "", pwd, CredentialPersistence.LocalMachine);
                }
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
                if (item is CommandOption option)
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
                var sourceDirectory = await GetOrCreateRootDirectoryItemAsync(sourceFileSystem, sourcePath, ct).ConfigureAwait(false);
                var targetDirectory = await GetOrCreateRootDirectoryItemAsync(targetFileSystem, targetPath, ct).ConfigureAwait(false);

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
            if (fileSystem is IAuthenticable authenticable)
            {
                await authenticable.LogInAsync(ct).ConfigureAwait(false);
            }

            return await fileSystem.GetOrCreateDirectoryItemAsync(path, ct).ConfigureAwait(false);
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