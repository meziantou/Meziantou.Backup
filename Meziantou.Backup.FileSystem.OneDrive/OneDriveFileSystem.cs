using System.Threading;
using System.Threading.Tasks;
using Meziantou.Backup.FileSystem.Abstractions;
using Meziantou.OneDrive;

namespace Meziantou.Backup.FileSystem.OneDrive
{
    public class OneDriveFileSystem : IFileSystem, IAuthenticable
    {
        public OneDriveClient Client { get; }

        public OneDriveFileSystem()
        {
            Client = new OneDriveClient();
            Client.RefreshTokenHandler = new CredentialManagerRefreshTokenHandler("Meziantou.Backup");
        }

        public async Task<IDirectoryInfo> GetOrCreateDirectoryItemAsync(string path, CancellationToken ct)
        {
            var item = await Client.CreateDirectoryAsync(path, ct);
            return new OneDriveFileInfo(item);
        }

        public void Initialize(string data)
        {
        }

        public Task LogInAsync(CancellationToken ct)
        {
            return Client.AuthenticateAsync(ct);
        }

        public Task LogOutAsync(CancellationToken ct)
        {
            return Client.LogOutAsync(ct);
        }
    }
}
