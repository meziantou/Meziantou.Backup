using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Meziantou.Backup.FileSystem.Abstractions;
using Meziantou.Framework.Win32;
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

            if (data.TryGetValue("ApplicationName", out object o) && o is string appName)
            {
                Client.RefreshTokenHandler = new CredentialManagerRefreshTokenHandler(appName, CredentialPersistence.LocalMachine);
            }

            if (data.TryGetValue("AuthenticateOnUnauthenticatedError", out o))
            {
                if (o is string value)
                {
                    if (bool.TryParse(value, out bool b))
                    {
                        Client.AuthenticateOnUnauthenticatedError = b;
                    }
                }
            }

            if (data.TryGetValue("UploadChunkSize", out o))
            {
                if (o is string value)
                {
                    if (int.TryParse(value, out int size))
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
