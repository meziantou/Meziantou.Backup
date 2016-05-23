using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;
using Meziantou.Backup.FileSystem.Abstractions;
using Microsoft.Extensions.Configuration;

namespace Meziantou.Backup.Console
{
    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            if (Debugger.IsAttached)
            {
                SafeMain(args);
            }
            else
            {
                try
                {
                    SafeMain(args);
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine(ex);
                }
            }
        }

        private static string GetValue(IConfigurationRoot configuration, string key, string defaultValue)
        {
            var section = configuration.GetSection(key);
            if (section == null)
                return defaultValue;

            return section.Value;
        }

        private static int GetValue(IConfigurationRoot configuration, string key, int defaultValue)
        {
            var section = configuration.GetSection(key);
            if (section == null)
                return defaultValue;

            int v;
            if (int.TryParse(section.Value, out v))
                return v;

            return defaultValue;
        }

        private static bool GetValue(IConfigurationRoot configuration, string key, bool defaultValue)
        {
            var section = configuration.GetSection(key);
            if (section == null)
                return defaultValue;

            bool v;
            if (bool.TryParse(section.Value, out v))
                return v;

            return defaultValue;
        }

        private static T GetValue<T>(IConfigurationRoot configuration, string key, T defaultValue) where T : struct
        {
            var section = configuration.GetSection(key);
            if (section == null)
                return defaultValue;

            T v;
            if (Enum.TryParse(section.Value, true, out v))
                return v;

            return defaultValue;
        }

        private static ProviderConfiguration GetProvider(IConfigurationRoot configuration, string prefix)
        {
            var provider = new ProviderConfiguration();
            provider.ProviderName = GetValue(configuration, prefix + nameof(ProviderConfiguration.ProviderName), null);
            provider.Path = GetValue(configuration, prefix + nameof(ProviderConfiguration.Path), null);

            var configurationPrefix = prefix + nameof(ProviderConfiguration.Configuration) + "-";
            var children = configuration.GetChildren();
            if (provider.Configuration == null)
            {
                provider.Configuration = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            }

            foreach (var child in children)
            {
                if (child.Key.StartsWith(configurationPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    provider.Configuration[child.Key.Substring(configurationPrefix.Length)] = child.Value;
                }
            }

            return provider;
        }

        private static void SafeMain(string[] args)
        {
            using (CancellationTokenSource cts = new CancellationTokenSource())
            {
                System.Console.CancelKeyPress += (sender, eventArgs) =>
                {
                    cts.Cancel();
                    eventArgs.Cancel = true;
                };

                var configuration = new ConfigurationBuilder()
                    .AddCommandLine(args)
                    .Build();

                var sourceProviderConfiguration = GetProvider(configuration, "source");
                var targetProviderConfiguration = GetProvider(configuration, "target");

                ServicePointManager.DefaultConnectionLimit = 8;
                Backup backup = new Backup();
                backup.Action += Backup_Action;
                backup.Error += Backup_Error;
                backup.Copying += Backup_Copying;
                backup.EqualityMethods = GetValue(configuration, "EqualityMethods", FileInfoEqualityMethods.Default);
                backup.RetryCount = GetValue(configuration, "RetryCount", 3);
                backup.CanCreateDirectories = GetValue(configuration, "CanCreateDirectories", true);
                backup.CanDeleteDirectories = GetValue(configuration, "CanDeleteDirectories", false);
                backup.CanCreateFiles = GetValue(configuration, "CanCreateFiles", true);
                backup.CanUpdateFiles = GetValue(configuration, "CanUpdateFiles", true);
                backup.CanDeleteFiles = GetValue(configuration, "CanDeleteFiles", false);

                var summary = new BackupSummary(backup);

                try
                {
                    var backupAsync = backup.RunAsync(sourceProviderConfiguration, targetProviderConfiguration, cts.Token);
                    var awaiter = backupAsync.GetAwaiter();
                    awaiter.GetResult();
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
            }
        }

        private static void Backup_Copying(object sender, FileCopyingEventArgs e)
        {
            float percent = ((float)e.CurrentPosition) / e.FileLength * 100f;
            System.Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId}] Copying ({percent:F1}% - {FriendlyFileLength(e.CurrentPosition)}/{FriendlyFileLength(e.FileLength)}): <{GetDisplayName(e.SourceItem)}> -> <{GetDisplayName(e.TargetItem)}>");
        }

        private static string FriendlyFileLength(long length)
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

        private static void Backup_Error(object sender, BackupErrorEventArgs e)
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

        private static void Backup_Action(object sender, BackupActionEventArgs e)
        {
            if (e.Method != FileInfoEqualityMethods.None)
            {
                System.Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId}] {e.Action} ({e.Method}): <{GetDisplayName(e.SourceItem)}> -> <{GetDisplayName(e.TargetItem)}>");
            }
            else
            {
                System.Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId}] {e.Action}: <{GetDisplayName(e.SourceItem)}> -> <{GetDisplayName(e.TargetItem)}>");
            }
        }

        private static string GetDisplayName(IFileSystemInfo item)
        {
            var fullName = item as IFullName;
            if (fullName != null)
                return fullName.FullName;

            return item?.Name;
        }
    }
}
