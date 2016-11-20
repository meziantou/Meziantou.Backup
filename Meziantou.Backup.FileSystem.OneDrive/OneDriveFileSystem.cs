using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Meziantou.Backup.FileSystem.Abstractions;
using Meziantou.OneDrive;
using Meziantou.OneDrive.Windows;

namespace Meziantou.Backup.FileSystem.OneDrive
{
    public class OneDriveFileSystem : IFileSystem, IAuthenticable
    {
        public OneDriveClient Client { get; }

        public OneDriveFileSystem()
        {
            var client = new OneDriveClient();
            client.ApplicationId = "000000004418B915";
            client.AuthorizationProvider = new AuthorizationCodeProvider();
            client.RefreshTokenHandler = new MemoryRefreshTokenHandler();

            Client = client;
        }

        public int UploadChunkSize { get; set; } = 5 * 1024 * 1024;

        public async Task<IDirectoryInfo> GetOrCreateDirectoryItemAsync(string path, CancellationToken ct)
        {
            if (!path.StartsWith("/"))
            {
                path = "/" + path;
            }

            var item = await Client.CreateDirectoryAsync(path, ct).ConfigureAwait(false);
            return new OneDriveFileInfo(this, item);
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

            if (data.TryGetValue("UploadChunkSize", out o))
            {
                var value = o as string;
                if (value != null)
                {
                    int size;
                    if (int.TryParse(value, out size))
                    {
                        UploadChunkSize = size;
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
