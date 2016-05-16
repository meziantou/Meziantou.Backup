using System;
using System.Threading;
using System.Threading.Tasks;

namespace Meziantou.Backup.FileSystem.Abstractions
{
    public interface IFileSystemInfo
    {
        string Name { get; }
        string Extension { get; }
        bool Exists { get; }
        DateTime CreationTimeUtc { get; }
        DateTime LastWriteTimeUtc { get; }
        Task DeleteAsync(CancellationToken ct);
    }
}