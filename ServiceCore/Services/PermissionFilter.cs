using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using ServiceCore.Data;
using System.Security.Claims;

namespace ServiceCore.Services
{
    public class PermissionFilter : IAsyncActionFilter
    {
        private readonly ServiceCoreDbContext _db;

        public PermissionFilter(ServiceCoreDbContext db)
        {
            _db = db;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (context.HttpContext.User.Identity?.IsAuthenticated == true)
            {
                var role = context.HttpContext.User.FindFirstValue(ClaimTypes.Role);
                if (string.IsNullOrEmpty(role))
                {
                    await next();
                    return;
                }

                // Get current controller and action to determine feature key
                var controller = context.RouteData.Values["controller"]?.ToString();
                var action = context.RouteData.Values["action"]?.ToString();

                // Map controllers to FeatureKeys (This can be expanded or made more dynamic)
                string featureKey = controller switch
                {
                    "Home" => "Dashboard",
                    "Tickets" => context.RouteData.Values["action"]?.ToString() == "Index" ? "Tickets_View" : "Tickets_Manage",
                    "TicketsManager" => "Tickets_Manager",
                    "Projects" => "Projects_Manage",
                    "Kanban" => "Kanban_Board",
                    "Users" => "Users_View",
                    "UsersManager" => "Users_Manage",
                    "Reports" => "Reports_View",
                    "Assets" => context.RouteData.Values["action"]?.ToString() == "Index" || context.RouteData.Values["action"]?.ToString() == "Details" ? "Assets_View" : "Assets_Manage",
                    "Approvals" => "Approvals_View", // Simple view-only or manage based on actions
                    "Admin" => action == "ManagePermissions" ? "Admin_Permissions" : "Admin_Metadata",
                    _ => string.Empty
                };

                // Simple mapping for demonstration - can be more granular
                if (!string.IsNullOrEmpty(featureKey))
                {
                    // Fail-safe: Always allow Admin
                    if (role == "Admin")
                    {
                        await next();
                        return;
                    }

                    var isAllowed = await _db.RolePermissions
                        .Where(p => p.RoleName == role && p.FeatureKey == featureKey)
                        .Select(p => (bool?)p.IsAllowed)
                        .FirstOrDefaultAsync();

                    // If a record exists and is false, deny access
                    if (isAllowed == false)
                    {
                        context.Result = new ViewResult { ViewName = "AccessDenied" };
                        return;
                    }
                }
            }

            await next();
        }
    }
}
