using System;
using System.Threading;
using Meziantou.Backup.FileSystem.Abstractions;

namespace Meziantou.Backup.Console
{
    internal class BackupSummary
    {
        private int _directoryCount;
        private int _directoryCreatedCount;
        private int _directoryDeletedCount;
        private int _fileDeletedCount;
        private int _fileUpdatedCount;
        private int _fileCreatedCount;
        private int _fileCount;
        private int _errorCount;

        public BackupSummary(Backup backup)
        {
            if (backup == null) throw new ArgumentNullException(nameof(backup));

            backup.Action += Backup_Action;
            backup.Error += Backup_Error;
        }

        private void Backup_Error(object sender, BackupErrorEventArgs e)
        {
            Interlocked.Increment(ref _errorCount);
        }

        private void Backup_Action(object sender, BackupActionEventArgs e)
        {
            switch (e.Action)
            {
                case BackupAction.Created:
                    if (e.SourceItem.IsDirectory())
                    {
                        Interlocked.Increment(ref _directoryCreatedCount);
                    }
                    else
                    {
                        Interlocked.Increment(ref _fileCreatedCount);
                    }
                    break;
                case BackupAction.Updated:
                    if (e.SourceItem.IsFile())
                    {
                        Interlocked.Increment(ref _fileUpdatedCount);
                    }
                    break;
                case BackupAction.Deleted:
                    if (e.SourceItem.IsDirectory())
                    {
                        Interlocked.Increment(ref _directoryDeletedCount);
                    }
                    else
                    {
                        Interlocked.Increment(ref _fileDeletedCount);
                    }
                    break;
                case BackupAction.Synchronized:
                    if (e.SourceItem.IsDirectory())
                    {
                        Interlocked.Increment(ref _directoryCount);
                    }
                    else
                    {
                        Interlocked.Increment(ref _fileCount);
                    }

                    break;
            }
        }

        public int DirectoryCount => _directoryCount;
        public int DirectoryCreatedCount => _directoryCreatedCount;
        public int DirectoryDeletedCount => _directoryDeletedCount;
        public int FileCount => _fileCount;
        public int FileCreatedCount => _fileCreatedCount;
        public int FileUpdatedCount => _fileUpdatedCount;
        public int FileDeletedCount => _fileDeletedCount;
        public int ErrorCount => _errorCount;
    }
}