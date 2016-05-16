using System.Threading;
using System.Threading.Tasks;
using Meziantou.Backup.FileSystem.Abstractions;
using Meziantou.OneDrive;

namespace Meziantou.Backup.FileSystem.OneDrive
{
    public class OneDriveFileSystem : IFileSystem
    {
        private readonly OneDriveClient _client;

        public OneDriveFileSystem()
        {
            _client = new OneDriveClient();
            _client.RefreshTokenHandler = new CredentialManagerRefreshTokenHandler("Meziantou.Backup");
        }

        public async Task<IDirectoryInfo> GetOrCreateDirectoryItemAsync(string path, CancellationToken ct)
        {
            var item = await _client.CreateDirectoryAsync(path, ct);
            return new OneDriveFileInfo(item);
        }

        public void Initialize(string data)
        {
        }

        public Task AuthenticateAsync(CancellationToken ct)
        {
            return _client.AuthenticateAsync(ct);
        }
    }
}
