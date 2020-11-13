using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Meziantou.Backup.FileSystem.Aes
{
    internal sealed class AesHeader
    {
        public AesHeader()
        {
        }

        public AesHeader(AesVersion version, byte[] iv)
        {
            Version = version;
            IV = iv;
        }

        public AesVersion Version { get; set; }
        public byte[] IV { get; set; }

        public static async Task<AesHeader> ReadAsync(Stream stream, CancellationToken ct)
        {
            var versionByte = new byte[sizeof(AesVersion)];
            var iv = new byte[16];

            await stream.ReadAsync(versionByte.AsMemory(), ct).ConfigureAwait(false);
            await stream.ReadAsync(iv.AsMemory(), ct).ConfigureAwait(false);

            var header = new AesHeader();
            header.Version = (AesVersion)versionByte[0];
            header.IV = iv;
            return header;
        }

        public static AesHeader Read(Stream stream)
        {
            var versionByte = new byte[sizeof(AesVersion)];
            var iv = new byte[16];

            stream.Read(versionByte, 0, versionByte.Length);
            stream.Read(iv, 0, iv.Length);

            var header = new AesHeader();
            header.Version = (AesVersion)versionByte[0];
            header.IV = iv;
            return header;
        }

        public async Task WriteAsync(Stream stream, CancellationToken ct)
        {
            await stream.WriteAsync(new[] { (byte)Version }, 0, 1, ct).ConfigureAwait(false);
            await stream.WriteAsync(IV, 0, IV.Length, ct).ConfigureAwait(false);
        }

        public void Write(Stream stream)
        {
            stream.Write(new[] { (byte)Version }, 0, 1);
            stream.Write(IV, 0, IV.Length);
        }
    }
}