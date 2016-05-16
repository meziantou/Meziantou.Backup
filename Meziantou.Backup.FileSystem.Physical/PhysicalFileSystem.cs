using System.Threading;
using System.Threading.Tasks;
using Alphaleonis.Win32.Filesystem;
using Meziantou.Backup.FileSystem.Abstractions;

namespace Meziantou.Backup.FileSystem.Physical
{
    public class PhysicalFileSystem : IFileSystem
    {
        public void Initialize(string data)
        {
        }

        public Task<IDirectoryInfo> GetOrCreateDirectoryItemAsync(string path, CancellationToken ct)
        {
            var di = new DirectoryInfo(path);
            if (!di.Exists)
            {
                di = Directory.CreateDirectory(path);
            }

            return Task.FromResult<IDirectoryInfo>(new PhysicalDirectoryInfo(di));
        }

        public Task AuthenticateAsync(CancellationToken ct)
        {
            return Task.CompletedTask;
        }
    }
}
