using System.Threading;
using System.Threading.Tasks;

namespace Meziantou.OneDrive
{
    public interface IRefreshTokenHandler
    {
        Task SaveRefreshTokenAsync(RefreshTokenInfo token, CancellationToken ct);
        Task<RefreshTokenInfo> RetrieveRefreshTokenAsync(CancellationToken ct);
        Task DeleteRefreshTokenAsync(CancellationToken ct);
    }
}