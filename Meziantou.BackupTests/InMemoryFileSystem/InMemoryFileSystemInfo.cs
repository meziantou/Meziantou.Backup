using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Meziantou.Backup.FileSystem.Abstractions;

namespace Meziantou.BackupTests.InMemoryFileSystem
{
    internal class InMemoryFileSystemInfo : IFileSystemInfo
    {
        public InMemoryFileSystemInfo(InMemoryDirectoryInfo parent)
        {
            Parent = parent;
        }

        public InMemoryDirectoryInfo Parent { get; }

        public bool IsDirectory => this is InMemoryDirectoryInfo;

        public string Name { get; set; }

        public string Extension => Path.GetExtension(Name);

        public bool Exists => true;

        public DateTime CreationTimeUtc { get; set; }

        public DateTime LastWriteTimeUtc { get; set; }

        public Task DeleteAsync(CancellationToken ct)
        {
            if (Parent == null)
                throw new Exception("Cannot delete root directory");

            Parent.Children.Remove(this);
            return Task.CompletedTask;
        }
    }
}