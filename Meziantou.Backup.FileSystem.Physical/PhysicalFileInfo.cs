using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Meziantou.Backup.FileSystem.Abstractions;

namespace Meziantou.Backup.FileSystem.Physical
{
    [DebuggerDisplay("{FullName}")]
    public class PhysicalFileInfo : PhysicalFileSystemInfo, IFileInfo
    {
        private readonly FileInfo _fi;
        
        public PhysicalFileInfo(FileInfo fi) : base(fi)
        {
            if (fi == null) throw new ArgumentNullException(nameof(fi));

            _fi = fi;
        }

        public override bool IsDirectory => false;

        public long Length => _fi.Length;

        public Task<Stream> OpenReadAsync(CancellationToken ct)
        {
            return Task.FromResult<Stream>(new FileStream(FullName, FileMode.Open, FileAccess.Read, FileShare.Read));
        }
    }
}