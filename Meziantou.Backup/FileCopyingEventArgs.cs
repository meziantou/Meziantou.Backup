using System;
using System.Collections.Generic;
using Meziantou.Backup.FileSystem.Abstractions;

namespace Meziantou.Backup
{
    public sealed class FileCopyingEventArgs : EventArgs
    {
        public FileCopyingEventArgs(IReadOnlyList<string> path, IFileSystemInfo sourceItem, IFileSystemInfo targetItem, long currentPosition, long fileLength)
        {
            Path = path;
            SourceItem = sourceItem;
            TargetItem = targetItem;
            CurrentPosition = currentPosition;
            FileLength = fileLength;
        }

        public IReadOnlyList<string> Path { get; }
        public IFileSystemInfo SourceItem { get; }
        public IFileSystemInfo TargetItem { get; }
        public long CurrentPosition { get; }
        public long FileLength { get; }
    }
}