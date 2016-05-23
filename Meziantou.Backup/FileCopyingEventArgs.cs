using System;
using Meziantou.Backup.FileSystem.Abstractions;

namespace Meziantou.Backup
{
    public class FileCopyingEventArgs : EventArgs
    {
        public FileCopyingEventArgs(IFileSystemInfo sourceItem, IFileSystemInfo targetItem, long currentPosition, long fileLength)
        {
            SourceItem = sourceItem;
            TargetItem = targetItem;
            CurrentPosition = currentPosition;
            FileLength = fileLength;
        }

        public IFileSystemInfo SourceItem { get; }
        public IFileSystemInfo TargetItem { get; }
        public long CurrentPosition { get; }
        public long FileLength { get; }
    }
}