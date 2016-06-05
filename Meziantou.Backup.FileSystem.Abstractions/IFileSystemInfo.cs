using System;
using System.Threading;
using System.Threading.Tasks;

namespace Meziantou.Backup.FileSystem.Abstractions
{
    public interface IFileSystemInfo
    {
        bool IsDirectory { get; }
        string Name { get; }
        DateTime CreationTimeUtc { get; }
        DateTime LastWriteTimeUtc { get; }
        Task DeleteAsync(CancellationToken ct);
    }
}