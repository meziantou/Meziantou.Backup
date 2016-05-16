using System;
using System.Threading;
using System.Threading.Tasks;

namespace Meziantou.OneDrive
{
    public class CredentialManagerRefreshTokenHandler : IRefreshTokenHandler
    {
        private readonly string _applicationName;

        public CredentialManagerRefreshTokenHandler(string applicationName)
        {
            if (applicationName == null) throw new ArgumentNullException(nameof(applicationName));

            _applicationName = applicationName;
        }

        public Task SaveRefreshTokenAsync(RefreshTokenInfo token, CancellationToken ct)
        {
            CredentialManager.WriteCredential(_applicationName, "UserName", token.RefreshToken);
            return Task.CompletedTask;
        }

        public Task<RefreshTokenInfo> RetrieveRefreshTokenAsync(CancellationToken ct)
        {
            Credential cred = CredentialManager.ReadCredential(_applicationName);
            if (cred != null)
            {
                return Task.FromResult(new RefreshTokenInfo(cred.Password));
            }

            return Task.FromResult((RefreshTokenInfo)null);
        }

        public Task DeleteRefreshTokenAsync(CancellationToken ct)
        {
            CredentialManager.DeleteCredential(_applicationName);
            return Task.CompletedTask;
        }
    }
}