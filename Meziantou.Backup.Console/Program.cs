using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            var configuration = new ConfigurationBuilder()
                .AddCommandLine(args)
                .Build();

            var sourceProviderConfiguration = GetProvider(configuration, "source");
            var targetProviderConfiguration = GetProvider(configuration, "target");

            Backup backup = new Backup();
            backup.Action += Backup_Action;
            backup.Error += Backup_Error;
            var summary = new BackupSummary(backup);

            var backupAsync = backup.RunAsync(sourceProviderConfiguration, targetProviderConfiguration, CancellationToken.None);
            var awaiter = backupAsync.GetAwaiter();
            awaiter.GetResult();

            System.Console.WriteLine("Directories: " + summary.DirectoryCount);
            System.Console.WriteLine("  Created: " + summary.DirectoryCreatedCount);
            System.Console.WriteLine("  Deleted: " + summary.DirectoryDeletedCount);

            System.Console.WriteLine("File: " + summary.FileCount);
            System.Console.WriteLine("  Created: " + summary.FileCreatedCount);
            System.Console.WriteLine("  Updated: " + summary.FileUpdatedCount);
            System.Console.WriteLine("  Deleted: " + summary.FileDeletedCount);
        }

        private static void Backup_Error(object sender, BackupErrorEventArgs e)
        {
            System.Console.WriteLine($"Retry:${e.RetryCount}: {e.Exception}");
        }

        private static void Backup_Action(object sender, BackupActionEventArgs e)
        {
            System.Console.WriteLine($"{e.Action}: <{GetDisplayName(e.SourceItem)}> -> <{GetDisplayName(e.TargetItem)}>");
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
