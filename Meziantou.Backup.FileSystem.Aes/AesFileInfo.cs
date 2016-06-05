using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Meziantou.Backup.FileSystem.Abstractions;

namespace Meziantou.Backup.FileSystem.Aes
{
    internal class AesFileInfo : AesFileSystemInfo, IFileInfo
    {
        private readonly IFileInfo _fileInfo;

        public AesFileInfo(AesFileSystem fileSystem, IFileInfo fileInfo) : base(fileSystem, fileInfo)
        {
            if (fileInfo == null) throw new ArgumentNullException(nameof(fileInfo));
            _fileInfo = fileInfo;
        }
        
        public long Length
        {
            get
            {
                // Cannot compute the actual file length without reading the file because of padding
                return -1;
            }
        }

        public async Task<Stream> OpenReadAsync(CancellationToken ct)
        {
            Stream stream = null;
            System.Security.Cryptography.Aes aes = null;
            ICryptoTransform decryptor = null;
            try
            {
                stream = await _fileInfo.OpenReadAsync(ct);
                var header = await AesHeader.ReadAsync(stream, ct);

                aes = CreateAes(header.Version);
                var key = FileSystem.ComputeKey(header.IV, header.Version);
                decryptor = CreateTransform(aes, key, header.IV, AesMode.Decrypt); // disposed by CryptoStream
                return new CryptoStream(stream, decryptor, CryptoStreamMode.Read);
            }
            catch
            {
                decryptor?.Dispose();
                stream?.Dispose();
                throw;
            }
            finally
            {
                aes?.Dispose();
            }
        }
    }
}