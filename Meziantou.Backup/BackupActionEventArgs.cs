using System.Collections.Generic;
using System.ComponentModel;
using Meziantou.Backup.FileSystem.Abstractions;

namespace Meziantou.Backup
{
    public class BackupActionEventArgs : CancelEventArgs
    {
        public BackupActionEventArgs(BackupAction action, IReadOnlyList<string> path, IFileSystemInfo sourceItem, IFileSystemInfo targetItem, FileInfoEqualityMethods method = FileInfoEqualityMethods.None)
        {
            SourceItem = sourceItem;
            TargetItem = targetItem;
            Action = action;
            Path = path;
            Method = method;
        }

        public IReadOnlyList<string> Path { get; }
        public IFileSystemInfo SourceItem { get; }
        public IFileSystemInfo TargetItem { get; }
        public BackupAction Action { get; }
        public FileInfoEqualityMethods Method { get; }
    }
}