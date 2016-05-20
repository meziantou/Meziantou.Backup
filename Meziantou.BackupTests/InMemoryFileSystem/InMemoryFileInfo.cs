using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Meziantou.Backup.FileSystem.Abstractions;

namespace Meziantou.BackupTests.InMemoryFileSystem
{
    internal class InMemoryFileInfo : InMemoryFileSystemInfo, IFileInfo
    {
        public byte[] Content { get; set; } = new byte[0];

        public long Length => Content.Length;

        public Task<Stream> OpenReadAsync(CancellationToken ct)
        {
            return Task.FromResult<Stream>(new MemoryStream(Content));
        }

        public InMemoryFileInfo(InMemoryDirectoryInfo parent) : base(parent)
        {
        }

        public void SetContent(Stream stream)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                ms.Seek(0, SeekOrigin.Begin);
                Content = ms.ToArray();
            }
        }
    }
}