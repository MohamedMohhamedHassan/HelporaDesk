using ServiceCore.Constants;
using ServiceCore.Models;
using Microsoft.EntityFrameworkCore;

namespace ServiceCore.Data
{
    public static class PermissionSeeder
    {
        public static async Task SeedAsync(ServiceCoreDbContext db)
        {
            var roles = await db.Roles.ToListAsync();
            if (!roles.Any()) return; // Should be seeded by main logic first

            // 1. Ensure Admin, Agent, User roles exist (redundant check but safe)
            // Assumes roles are already seeded by Program.cs main block

            var allPermissions = Permissions.All;

            // Seed Admin Permissions (All True)
            await EnsurePermissionsForRole(db, "Admin", allPermissions, true);

            // Seed Agent Permissions (Tickets + Assets + Users View)
            var agentPermissions = new List<string> 
            { 
                Permissions.Tickets_View, Permissions.Tickets_Create, Permissions.Tickets_Edit, Permissions.Tickets_Manage,
                Permissions.Assets_View, Permissions.Assets_Manage,
                Permissions.Users_View,
                Permissions.Solutions_View, Permissions.Solutions_Manage,
                Permissions.Contracts_View
            };
            await EnsurePermissionsForRole(db, "Agent", agentPermissions, true);

            // Seed User Permissions (Basic View/Create)
            var userPermissions = new List<string>
            {
                Permissions.Tickets_View, Permissions.Tickets_Create,
                Permissions.Solutions_View
            };
            await EnsurePermissionsForRole(db, "User", userPermissions, true);
        }

        private static async Task EnsurePermissionsForRole(ServiceCoreDbContext db, string roleName, List<string> permissions, bool isAllowed)
        {
            var existing = await db.RolePermissions
                .Where(p => p.RoleName == roleName)
                .ToListAsync();

            foreach (var perm in permissions)
            {
                if (!existing.Any(p => p.FeatureKey == perm))
                {
                    db.RolePermissions.Add(new RolePermission
                    {
                        RoleName = roleName,
                        FeatureKey = perm,
                        IsAllowed = isAllowed
                    });
                }
            }
            await db.SaveChangesAsync();
        }
    }
}
