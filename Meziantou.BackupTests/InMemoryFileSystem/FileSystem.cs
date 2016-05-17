using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Meziantou.Backup.FileSystem.Abstractions;

namespace Meziantou.BackupTests.InMemoryFileSystem
{
    internal class FileSystem : IFileSystem
    {
        private readonly InMemoryDirectoryInfo _root = new InMemoryDirectoryInfo(null);

        public Task<IDirectoryInfo> GetOrCreateDirectoryItemAsync(string path, CancellationToken ct)
        {
            return Task.FromResult<IDirectoryInfo>(_root);
        }

        public void Initialize(IDictionary<string, object> data)
        {
        }

        public Task AuthenticateAsync(CancellationToken ct)
        {
            return Task.CompletedTask;
        }

        public void AddItem(string path)
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
                    di.CreateFileAsync(part, null, 0, CancellationToken.None).Wait();
                }
                else
                {
                    di = di.CreateDirectoryAsync(part, CancellationToken.None).Result;
                }
            }
        }

        public bool HasItem(string path)
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
                    return di.GetFile(part) != null;
                }
                else
                {
                    di = di.GetDirectory(part);
                }

                if (di == null)
                    return false;
            }

            return true;
        }
    }
}
