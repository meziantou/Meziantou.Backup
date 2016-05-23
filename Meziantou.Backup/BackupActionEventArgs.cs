using System.ComponentModel;
using Meziantou.Backup.FileSystem.Abstractions;

namespace Meziantou.Backup
{
    public class BackupActionEventArgs : CancelEventArgs
    {
        public BackupActionEventArgs(BackupAction action, IFileSystemInfo sourceItem, IFileSystemInfo targetItem, FileInfoEqualityMethods method = FileInfoEqualityMethods.None)
        {
            SourceItem = sourceItem;
            TargetItem = targetItem;
            Action = action;
            Method = method;
        }

        public IFileSystemInfo SourceItem { get; }
        public IFileSystemInfo TargetItem { get; }
        public BackupAction Action { get; }
        public FileInfoEqualityMethods Method { get; }
    }
}