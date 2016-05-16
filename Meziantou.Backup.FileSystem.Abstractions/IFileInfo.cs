using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Meziantou.Backup.FileSystem.Abstractions
{
    public interface IFileInfo : IFileSystemInfo
    {
        long Length { get; }

        Task<Stream> OpenReadAsync(CancellationToken ct);
    }
}