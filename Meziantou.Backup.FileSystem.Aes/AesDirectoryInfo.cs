using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Meziantou.Backup.FileSystem.Abstractions;

namespace Meziantou.Backup.FileSystem.Aes
{
    internal class AesDirectoryInfo : AesFileSystemInfo, IDirectoryInfo
    {
        private readonly IDirectoryInfo _directoryInfo;

        public AesDirectoryInfo(AesFileSystem fileSystem, IDirectoryInfo directoryInfo) : base(fileSystem, directoryInfo)
        {
            _directoryInfo = directoryInfo;
            if (directoryInfo == null) throw new ArgumentNullException(nameof(directoryInfo));
        }

        private IFileSystemInfo Convert(IFileSystemInfo fsi)
        {
            if (fsi == null)
                return null;

            if (fsi is IFileInfo fileInfo && fileInfo.IsFile())
            {
                return new AesFileInfo(FileSystem, fileInfo);
            }

            if (fsi is IDirectoryInfo directoryInfo && directoryInfo.IsDirectory())
            {
                return new AesDirectoryInfo(FileSystem, directoryInfo);
            }

            return null;
        }

        public async Task<IReadOnlyCollection<IFileSystemInfo>> GetItemsAsync(CancellationToken ct)
        {
            var items = await _directoryInfo.GetItemsAsync(ct).ConfigureAwait(false);
            return items.Select(Convert).ToList();
        }

        public async Task<IFileInfo> CreateFileAsync(string name, Stream stream, long length, CancellationToken ct)
        {
            if (FileSystem.EncryptFileName)
            {
                name = EncryptName(name);
            }

            using (var aes = CreateAes(FileSystem.Version))
            {
                using (var headerStream = new MemoryStream())
                {
                    var aesHeader = new AesHeader(FileSystem.Version, aes.IV);
                    aesHeader.Write(headerStream);
                    headerStream.Seek(0, SeekOrigin.Begin);

                    var key = FileSystem.ComputeKey(aes.IV, FileSystem.Version);
                    ICryptoTransform encryptor = CreateTransform(aes, key, aes.IV, AesMode.Encrypt);
                    using (var cryptoStream = new CryptoStream(stream, encryptor, CryptoStreamMode.Read))
                    using (var concat = new StreamEnumerator(new Stream[] { headerStream, cryptoStream }, true))
                    {
                        var fileInfo = await _directoryInfo.CreateFileAsync(name, concat, length, ct).ConfigureAwait(false);
                        return new AesFileInfo(FileSystem, fileInfo);
                    }
                }
            }
        }

        public async Task<IDirectoryInfo> CreateDirectoryAsync(string name, CancellationToken ct)
        {
            if (FileSystem.EncryptDirectoryName)
            {
                name = EncryptName(name);
            }

            var di = await _directoryInfo.CreateDirectoryAsync(name, ct).ConfigureAwait(false);
            return new AesDirectoryInfo(FileSystem, di);
        }
    }
}