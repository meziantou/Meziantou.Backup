using System.Threading;
using System.Threading.Tasks;

namespace Meziantou.OneDrive
{
    internal class MemoryRefreshTokenHandler : IRefreshTokenHandler
    {
        private RefreshTokenInfo _token;

        public Task SaveRefreshTokenAsync(RefreshTokenInfo token, CancellationToken ct)
        {
            _token = token;
            return Task.CompletedTask;
        }

        public Task<RefreshTokenInfo> RetrieveRefreshTokenAsync(CancellationToken ct)
        {
            return Task.FromResult(_token);
        }

        public Task DeleteRefreshTokenAsync(CancellationToken ct)
        {
            _token = null;
            return Task.CompletedTask;
        }
    }
}