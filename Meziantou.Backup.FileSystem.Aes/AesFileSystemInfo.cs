using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Meziantou.Backup.FileSystem.Abstractions;

namespace Meziantou.Backup.FileSystem.Aes
{
    internal abstract class AesFileSystemInfo : IFileSystemInfo
    {
        private readonly IFileSystemInfo _fileSystemInfo;

        protected AesFileSystem FileSystem { get; }

        protected AesFileSystemInfo(AesFileSystem fileSystem, IFileSystemInfo fileSystemInfo)
        {
            if (fileSystemInfo == null) throw new ArgumentNullException(nameof(fileSystemInfo));
            FileSystem = fileSystem;
            _fileSystemInfo = fileSystemInfo;
        }

        public bool IsDirectory => _fileSystemInfo.IsDirectory;
        public DateTime CreationTimeUtc => _fileSystemInfo.CreationTimeUtc;
        public DateTime LastWriteTimeUtc => _fileSystemInfo.LastWriteTimeUtc;

        public string Name
        {
            get
            {
                if (_fileSystemInfo.IsDirectory() && FileSystem.EncryptDirectoryName)
                {
                    return DecryptName(_fileSystemInfo.Name);
                }

                if (_fileSystemInfo.IsFile() && FileSystem.EncryptFileName)
                {
                    return DecryptName(_fileSystemInfo.Name);
                }

                return _fileSystemInfo.Name;
            }
        }

        public Task DeleteAsync(CancellationToken ct)
        {
            return _fileSystemInfo.DeleteAsync(ct);
        }

        protected virtual System.Security.Cryptography.Aes CreateAes(AesVersion version)
        {
            System.Security.Cryptography.Aes aes = System.Security.Cryptography.Aes.Create();
            switch (version)
            {
                case AesVersion.Aes128:
                    aes.KeySize = 128;
                    break;
                case AesVersion.Aes256:
                    aes.KeySize = 256;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return aes;
        }

        protected virtual ICryptoTransform CreateTransform(System.Security.Cryptography.Aes aes, byte[] key, byte[] iv, AesMode mode)
        {
            if (aes == null) throw new ArgumentNullException(nameof(aes));
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (iv == null) throw new ArgumentNullException(nameof(iv));

            switch (mode)
            {
                case AesMode.Decrypt:
                    return aes.CreateDecryptor(key, iv);
                case AesMode.Encrypt:
                    return aes.CreateEncryptor(key, iv);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected virtual string EncryptName(string name)
        {
            var bytes = Encoding.UTF8.GetBytes(name);

            var aesVersion = FileSystem.Version;
            using var aes = CreateAes(aesVersion);
            var iv = aes.IV;

            var key = FileSystem.ComputeKey(iv, aesVersion);
            using var tranform = aes.CreateEncryptor(key, iv);
            using var outputStream = new MemoryStream();
            var aesHeader = new AesHeader(aesVersion, iv);
            aesHeader.Write(outputStream);

            var encryptedBytes = tranform.TransformFinalBlock(bytes, 0, bytes.Length);
            outputStream.Write(encryptedBytes, 0, encryptedBytes.Length);

            return Convert.ToBase64String(outputStream.ToArray()).Replace('/', '-');
        }

        protected virtual string DecryptName(string name)
        {
            try
            {
                byte[] bytes = Convert.FromBase64String(name.Replace('-', '/'));
                using var ms = new MemoryStream(bytes);
                var header = AesHeader.Read(ms);

                var aesVersion = header.Version;
                using var aes = CreateAes(aesVersion);
                var iv = header.IV;
                var key = FileSystem.ComputeKey(iv, aesVersion);

                using var tranform = aes.CreateDecryptor(key, iv);
                using var outputStream = new MemoryStream();
                using (var cryptoStream = new CryptoStream(outputStream, tranform, CryptoStreamMode.Write))
                {
                    ms.CopyTo(cryptoStream);
                }

                return Encoding.UTF8.GetString(outputStream.ToArray());
            }
            catch
            {
                return name;
            }
        }
    }
}