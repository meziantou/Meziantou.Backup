using System.ComponentModel;
using Meziantou.Backup.FileSystem.Abstractions;

namespace Meziantou.Backup
{
    public class BackupActionEventArgs : CancelEventArgs
    {
        public BackupActionEventArgs(BackupAction action, IFileSystemInfo sourceItem, IFileSystemInfo targetItem)
        {
            SourceItem = sourceItem;
            TargetItem = targetItem;
            Action = action;
        }

        public IFileSystemInfo SourceItem { get; }
        public IFileSystemInfo TargetItem { get; }
        public BackupAction Action { get; }
    }
}