using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Meziantou.Backup.FileSystem.Abstractions;

namespace Meziantou.Backup.FileSystem.InMemory
{
    public sealed class InMemoryFileInfo : InMemoryFileSystemInfo, IFileInfo
    {
        public byte[] Content { get; set; } = System.Array.Empty<byte>();

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
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            ms.Seek(0, SeekOrigin.Begin);
            Content = ms.ToArray();
        }
    }
}