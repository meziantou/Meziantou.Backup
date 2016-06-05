using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Meziantou.Backup.FileSystem.Abstractions;
using Renci.SshNet.Sftp;

namespace Meziantou.Backup.FileSystem.Sftp
{
    public class SftpFileInfo : IDirectoryInfo, IFileInfo, IFullName
    {
        private readonly SftpFileSystem _fileSystem;
        private readonly SftpFile _file;

        public SftpFileInfo(SftpFileSystem fileSystem, SftpFile file)
        {
            _fileSystem = fileSystem;
            _file = file;
        }

        public bool IsDirectory => _file.IsDirectory;
        public string Name => _file.Name;
        public string FullName => _file.FullName;
        public DateTime CreationTimeUtc => _file.LastWriteTimeUtc;
        public DateTime LastWriteTimeUtc => _file.LastWriteTimeUtc;
        public long Length => _file.Length;

        public Task DeleteAsync(CancellationToken ct)
        {
            _file.Delete();
            return Task.CompletedTask;
        }

        public async Task<IReadOnlyCollection<IFileSystemInfo>> GetItemsAsync(CancellationToken ct)
        {
            return await _fileSystem.ListDirectoryAsync(FullName).ConfigureAwait(false);
        }

        public async Task<IFileInfo> CreateFileAsync(string name, Stream stream, long length, CancellationToken ct)
        {
            return await _fileSystem.UploadFileAsync(FullName, name, stream).ConfigureAwait(false);
        }

        public Task<IDirectoryInfo> CreateDirectoryAsync(string name, CancellationToken ct)
        {
            var sftpFileInfo = _fileSystem.CreateDirectory(FullName, name);
            return Task.FromResult<IDirectoryInfo>(sftpFileInfo);
        }

        public Task<Stream> OpenReadAsync(CancellationToken ct)
        {
            return _fileSystem.DownloadFileAsync(FullName);
        }
    }
}
