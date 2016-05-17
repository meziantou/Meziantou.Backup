using System.Threading;
using System.Threading.Tasks;

namespace Meziantou.Backup.FileSystem.Abstractions
{
    public interface IFileSystem
    {
        Task<IDirectoryInfo> GetOrCreateDirectoryItemAsync(string path, CancellationToken ct);
        void Initialize(string data);
    }
}