using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Meziantou.Backup.FileSystem.Abstractions;

namespace Meziantou.Backup.FileSystem.InMemory
{
    public sealed class InMemoryDirectoryInfo : InMemoryFileSystemInfo, IDirectoryInfo
    {
        private readonly StringComparison _stringComparison = StringComparison.OrdinalIgnoreCase;

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
            var file = GetFile(name);
            if (file == null)
            {
                file = new InMemoryFileInfo(this);
                _children.Add(file);
            }

            file.Name = name;
            if (stream != null)
            {
                file.SetContent(stream);
            }

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