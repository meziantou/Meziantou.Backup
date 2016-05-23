using System.Collections.Generic;
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
        }

        public async Task<IDirectoryInfo> GetOrCreateDirectoryItemAsync(string path, CancellationToken ct)
        {
            var item = await Client.CreateDirectoryAsync(path, ct).ConfigureAwait(false);
            return new OneDriveFileInfo(item);
        }

        public void Initialize(IDictionary<string, object> data)
        {
            if (data == null)
                return;

            object o;
            if (data.TryGetValue("ApplicationName", out o))
            {
                var appName = o as string;
                if (appName != null)
                {
                    Client.RefreshTokenHandler = new CredentialManagerRefreshTokenHandler(appName);
                }
            }

            if (data.TryGetValue("AuthenticateOnUnauthenticatedError", out o))
            {
                var value = o as string;
                if (value != null)
                {
                    bool b;
                    if (bool.TryParse(value, out b))
                    {
                        Client.AuthenticateOnUnauthenticatedError = b;
                    }
                }
            }
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
