using System.Security.Claims;
using System.Threading.Tasks;

namespace ServiceCore.Services
{
    public interface IPermissionService
    {
        Task<bool> HasPermissionAsync(ClaimsPrincipal user, string permission);
        Task<bool> HasPermissionAsync(string role, string permission);
    }
}
