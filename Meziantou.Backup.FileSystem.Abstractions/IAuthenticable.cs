using System.Threading;
using System.Threading.Tasks;

namespace Meziantou.Backup.FileSystem.Abstractions
{
    public interface IAuthenticable
    {
        Task LogInAsync(CancellationToken ct);
        Task LogOutAsync(CancellationToken ct);
    }
}