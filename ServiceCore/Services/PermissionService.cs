using Microsoft.EntityFrameworkCore;
using ServiceCore.Constants;
using ServiceCore.Data;
using System.Security.Claims;

namespace ServiceCore.Services
{
    public class PermissionService : IPermissionService
    {
        private readonly ServiceCoreDbContext _db;

        public PermissionService(ServiceCoreDbContext db)
        {
            _db = db;
        }

        public async Task<bool> HasPermissionAsync(ClaimsPrincipal user, string permission)
        {
            if (user?.Identity?.IsAuthenticated != true) return false;

            var role = user.FindFirstValue(ClaimTypes.Role);
            if (string.IsNullOrEmpty(role)) return false;

            return await HasPermissionAsync(role, permission);
        }

        public async Task<bool> HasPermissionAsync(string role, string permission)
        {
            // Admin always has access
            if (role == "Admin") return true;

            // Check database for explicit permission
            var isAllowed = await _db.RolePermissions
                .Where(p => p.RoleName == role && p.FeatureKey == permission)
                .Select(p => (bool?)p.IsAllowed)
                .FirstOrDefaultAsync();

            // Default to FALSE if not found or explicitly set to false
            return isAllowed ?? false;
        }
    }
}
