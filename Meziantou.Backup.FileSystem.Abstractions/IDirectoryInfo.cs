using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Meziantou.Backup.FileSystem.Abstractions
{
    public interface IDirectoryInfo : IFileSystemInfo
    {
        Task<IReadOnlyCollection<IFileSystemInfo>> GetItemsAsync(CancellationToken ct);
        Task<IFileInfo> CreateFileAsync(string name, Stream stream, long length, CancellationToken ct);
        Task<IDirectoryInfo> CreateDirectoryAsync(string name, CancellationToken ct);
    }
}