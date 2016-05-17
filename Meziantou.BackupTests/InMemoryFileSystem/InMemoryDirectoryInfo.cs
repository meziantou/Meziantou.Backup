using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Meziantou.Backup.FileSystem.Abstractions;

namespace Meziantou.BackupTests.InMemoryFileSystem
{
    internal class InMemoryDirectoryInfo : InMemoryFileSystemInfo, IDirectoryInfo
    {
        private StringComparison _stringComparison = StringComparison.OrdinalIgnoreCase;

        private readonly List<InMemoryFileSystemInfo> _children = new List<InMemoryFileSystemInfo>();

        public IList<InMemoryFileSystemInfo> Children => _children;

        public Task<IReadOnlyCollection<IFileSystemInfo>> GetItemsAsync(CancellationToken ct)
        {
            return Task.FromResult<IReadOnlyCollection<IFileSystemInfo>>(_children);
        }

        public InMemoryFileInfo GetFile(string name)
        {
            return Children.OfType<InMemoryFileInfo>().FirstOrDefault(_ => string.Equals(_.Name, name, _stringComparison));
        }

        public InMemoryDirectoryInfo GetDirectory(string name)
        {
            return Children.OfType<InMemoryDirectoryInfo>().FirstOrDefault(_ => string.Equals(_.Name, name, _stringComparison));
        }

        public Task<IFileInfo> CreateFileAsync(string name, Stream stream, long length, CancellationToken ct)
        {
            var item = GetFile(name);
            if (item != null)
                return Task.FromResult<IFileInfo>(item);

            var file = new InMemoryFileInfo(this);
            file.Name = name;

            if (stream != null)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    stream.CopyTo(ms);
                    ms.Seek(0, SeekOrigin.Begin);
                    file.Content = ms.ToArray();
                }
            }

            _children.Add(file);
            return Task.FromResult<IFileInfo>(file);
        }

        public Task<IDirectoryInfo> CreateDirectoryAsync(string name, CancellationToken ct)
        {
            var item = GetDirectory(name);
            if (item != null)
                return Task.FromResult<IDirectoryInfo>(item);

            var directory = new InMemoryDirectoryInfo(this);
            directory.Name = name;

            _children.Add(directory);
            return Task.FromResult<IDirectoryInfo>(directory);
        }

        public InMemoryDirectoryInfo(InMemoryDirectoryInfo parent) : base(parent)
        {
        }
    }
}