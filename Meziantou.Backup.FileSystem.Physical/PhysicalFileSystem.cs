using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Meziantou.Backup.FileSystem.Abstractions;

namespace Meziantou.Backup.FileSystem.Physical
{
    public class PhysicalFileSystem : IFileSystem
    {
        public Task<IDirectoryInfo> GetOrCreateDirectoryItemAsync(string path, CancellationToken ct)
        {
            var di = new DirectoryInfo(path);
            if (!di.Exists)
            {
                di = Directory.CreateDirectory(path);
            }
            
            return Task.FromResult<IDirectoryInfo>(new PhysicalDirectoryInfo(di));
        }

        public void Initialize(IDictionary<string, object> data)
        {
        }
    }
}
