using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Meziantou.Backup.FileSystem.Abstractions;

namespace Meziantou.Backup.Console
{
    internal class ConsoleLogger
    {
        private readonly Backup _backup;
        public ConsoleLoggerLevel Level { get; set; }

        public ConsoleLogger(Backup backup)
        {
            if (backup == null) throw new ArgumentNullException(nameof(backup));
            _backup = backup;

            backup.Action += Backup_Action;
            backup.Copying += Backup_Copying;
            backup.Error += Backup_Error;
        }

        private void Backup_Error(object sender, BackupErrorEventArgs e)
        {
            if ((this.Level & ConsoleLoggerLevel.Error) != 0)
            {
                SetConsoleColor(ConsoleColor.Red, () =>
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
                });
            }
        }

        private void Backup_Copying(object sender, FileCopyingEventArgs e)
        {
            if ((Level & ConsoleLoggerLevel.FileCopyProgress) != 0)
            {
                float percent = ((float)e.CurrentPosition) / e.FileLength * 100f;
                System.Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId}] Copying ({percent:F1}% - {FriendlyFileLength(e.CurrentPosition)}/{FriendlyFileLength(e.FileLength)}): {GetDisplayName(e, e.SourceItem)}");
            }
        }

        private void Backup_Action(object sender, BackupActionEventArgs e)
        {
            if (!CanLog(e))
                return;

            if (e.Method != FileInfoEqualityMethods.None)
            {
                System.Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId}] {e.Action} ({e.Method}): {GetDisplayName(e, e.SourceItem)}");
            }
            else
            {
                System.Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId}] {e.Action}: {GetDisplayName(e, e.SourceItem)}");
            }
        }

        private void SetConsoleColor(ConsoleColor color, Action action)
        {
            var currentColor = System.Console.ForegroundColor;
            System.Console.ForegroundColor = color;
            try
            {
                action();
            }
            finally
            {
                System.Console.ForegroundColor = currentColor;
            }
        }

        private bool CanLog(BackupActionEventArgs e)
        {
            if (e.Action == BackupAction.Synchronized ||
                e.Action == BackupAction.Created ||
                e.Action == BackupAction.Updated ||
                e.Action == BackupAction.Deleted)
                return false;

            if (e.Action == BackupAction.Synchronizing && Level.HasFlag(ConsoleLoggerLevel.DirectorySynchronizing) && e.SourceItem.IsFile())
                return true;

            if (e.Action == BackupAction.Creating && Level.HasFlag(ConsoleLoggerLevel.FileCreating) && e.SourceItem.IsFile())
                return true;

            if (e.Action == BackupAction.Updating && Level.HasFlag(ConsoleLoggerLevel.FileUpdating) && e.SourceItem.IsFile())
                return true;

            if (e.Action == BackupAction.Deleting && Level.HasFlag(ConsoleLoggerLevel.FileDeleting) && e.SourceItem.IsFile())
                return true;

            if (e.Action == BackupAction.Creating && Level.HasFlag(ConsoleLoggerLevel.DirectoryCreating) && e.SourceItem.IsDirectory())
                return true;

            if (e.Action == BackupAction.Deleting && Level.HasFlag(ConsoleLoggerLevel.DirectoryDeleting) && e.SourceItem.IsDirectory())
                return true;

            return false;
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

        private string GetDisplayName(FileCopyingEventArgs e, IFileSystemInfo main)
        {
            return GetDisplayName(e.Path, e.SourceItem, e.TargetItem, main);
        }

        private string GetDisplayName(BackupActionEventArgs e, IFileSystemInfo main)
        {
            return GetDisplayName(e.Path, e.SourceItem, e.TargetItem, main);
        }

        private string GetDisplayName(IReadOnlyCollection<string> path, IFileSystemInfo source, IFileSystemInfo target, IFileSystemInfo main)
        {
            if (Level.HasFlag(ConsoleLoggerLevel.FullPath))
            {
                return $"\"{GetFullName(path, source, false)}\" -> \"{GetFullName(path, target, false)}\"";
            }
            else
            {
                return GetFullName(path, main, true);
            }
        }

        private string GetFullName(IEnumerable<string> path, IFileSystemInfo item, bool relative)
        {
            if (!relative)
            {
                var fullName = item as IFullName;
                if (fullName != null)
                    return fullName.FullName;
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("/");
            foreach (var part in path)
            {
                sb.Append(part);
                sb.Append("/");
            }

            sb.Append(item?.Name);
            return sb.ToString();
        }
    }
}