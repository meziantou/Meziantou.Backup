using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Meziantou.Backup.FileSystem.Abstractions;

namespace Meziantou.Backup.FileSystem.Physical
{
    [DebuggerDisplay("{FullName}")]
    public abstract class PhysicalFileSystemInfo : IFileSystemInfo, IFullName
    {
        private readonly FileSystemInfo _fsi;

        protected PhysicalFileSystemInfo(FileSystemInfo fsi)
        {
            if (fsi == null) throw new ArgumentNullException(nameof(fsi));

            _fsi = fsi;
        }

        public abstract bool IsDirectory { get; }
        public string Name => _fsi.Name;
        public string FullName => _fsi.FullName;
        public DateTime CreationTimeUtc => _fsi.CreationTimeUtc;
        public DateTime LastWriteTimeUtc => _fsi.LastWriteTimeUtc;

        public Task DeleteAsync(CancellationToken ct)
        {
            _fsi.Delete();
            return Task.FromResult(0);
        }
    }
}