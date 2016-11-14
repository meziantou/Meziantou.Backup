using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Meziantou.Backup.FileSystem.Abstractions;

namespace Meziantou.Backup.FileSystem.Aes
{
    public class AesFileSystem : IFileSystem
    {
        private readonly IFileSystem _fileSystem;
        
        public AesVersion Version { get; set; }
        public string Password { get; set; }
        public bool EncryptFileName { get; set; }
        public bool EncryptDirectoryName { get; set; }
        public int IterationCount { get; set; } = 10000;

        public AesFileSystem(IFileSystem fileSystem)
        {
            if (fileSystem == null) throw new ArgumentNullException(nameof(fileSystem));
            _fileSystem = fileSystem;
        }

        internal byte[] ComputeKey(byte[] salt, AesVersion version)
        {
            if (salt == null) throw new ArgumentNullException(nameof(salt));

            using (var deriveBytes = new Rfc2898DeriveBytes(Password, salt, IterationCount))
            {
                int length = version == AesVersion.Aes128 ? 16 : 32;
                return deriveBytes.GetBytes(length);
            }
        }

        public async Task<IDirectoryInfo> GetOrCreateDirectoryItemAsync(string path, CancellationToken ct)
        {
            var di = await _fileSystem.GetOrCreateDirectoryItemAsync(path, ct);
            return new AesDirectoryInfo(this, di);
        }

        public void Initialize(IDictionary<string, object> data)
        {
            _fileSystem.Initialize(data);
        }
    }
}