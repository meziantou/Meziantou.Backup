using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Meziantou.Backup.FileSystem.Abstractions;
using System.Linq;

namespace Meziantou.Backup.FileSystem.InMemory
{
    public sealed class InMemoryFileSystem : IFileSystem
    {
        private readonly InMemoryDirectoryInfo _root = new InMemoryDirectoryInfo(null);

        public Task<IDirectoryInfo> GetOrCreateDirectoryItemAsync(string path, CancellationToken ct)
        {
            return Task.FromResult<IDirectoryInfo>(_root);
        }

        public void Initialize(IDictionary<string, object> data)
        {
        }

        public void AddItem(string path, byte[] content = null)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            bool isDirectory = path.EndsWith("/") || path.EndsWith("\\");
            var parts = path.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            IDirectoryInfo di = _root;
            for (int index = 0; index < parts.Length; index++)
            {
                var part = parts[index];
                if (!isDirectory && index == parts.Length - 1)
                {
                    di.CreateFileAsync(part, content ?? Array.Empty<byte>(), CancellationToken.None).Wait();
                }
                else
                {
                    di = di.CreateDirectoryAsync(part, CancellationToken.None).Result;
                }
            }
        }

        public bool ContainsItem(string path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            return GetItem(path) != null;
        }

        public bool HasItems()
        {
            return _root.Children.Any();
        }

        public InMemoryFileSystemInfo GetItem(string path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            bool isDirectory = path.EndsWith("/") || path.EndsWith("\\");
            var parts = path.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            var di = _root;
            for (int index = 0; index < parts.Length; index++)
            {
                var part = parts[index];
                if (!isDirectory && index == parts.Length - 1)
                {
                    return di.GetFile(part);
                }
                else
                {
                    di = di.GetDirectory(part);
                }
            }

            return di;
        }

        public InMemoryFileInfo GetFile(string path)
        {
            return GetItem(path) as InMemoryFileInfo;
        }

        public InMemoryDirectoryInfo GetDirectory(string path)
        {
            return GetItem(path) as InMemoryDirectoryInfo;
        }
    }
}
