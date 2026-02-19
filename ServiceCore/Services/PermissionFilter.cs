using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using ServiceCore.Constants;
using System.Security.Claims;

namespace ServiceCore.Services
{
    public class PermissionFilter : IAsyncActionFilter
    {
        private readonly IServiceProvider _serviceProvider;

        public PermissionFilter(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (context.HttpContext.User.Identity?.IsAuthenticated == true)
            {
                var role = context.HttpContext.User.FindFirstValue(ClaimTypes.Role);
                if (string.IsNullOrEmpty(role) || role == "Admin")
                {
                    await next();
                    return;
                }

                // Get current controller and action to determine feature key
                var controller = context.RouteData.Values["controller"]?.ToString();
                var action = context.RouteData.Values["action"]?.ToString();

                // Map controllers to FeatureKeys
                // Ideally this mapping should be more robust (attribute based?), but for now map broadly
                string featureKey = GetFeatureKeyForRequest(controller, action);

                if (!string.IsNullOrEmpty(featureKey))
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();
                        var hasPermission = await permissionService.HasPermissionAsync(context.HttpContext.User, featureKey);

                        if (!hasPermission)
                        {
                            context.Result = new ViewResult { ViewName = "AccessDenied" };
                            return;
                        }
                    }
                }
            }

            await next();
        }

        private string GetFeatureKeyForRequest(string controller, string action)
        {
            return controller switch
            {
                "Home" => "Dashboard", // Allow everyone?
                "Tickets" => action == "Index" || action == "Details" ? Permissions.Tickets_View : 
                             action == "Create" ? Permissions.Tickets_Create :
                             action == "Edit" ? Permissions.Tickets_Edit :
                             action == "Delete" ? Permissions.Tickets_Delete : Permissions.Tickets_Manage,
                "TicketsManager" => Permissions.Tickets_Manage,
                "Projects" => action == "Index" ? Permissions.Projects_View : Permissions.Projects_Manage,
                "Kanban" => Permissions.Kanban_View,
                "Users" => Permissions.Users_View,
                "UsersManager" => Permissions.Users_Manage,
                "Reports" => "Reports_View", // Needs constant
                "Assets" => action == "Index" || action == "Details" ? Permissions.Assets_View : Permissions.Assets_Manage,
                "Approvals" => "Approvals_View",
                "Admin" => action == "ManagePermissions" ? Permissions.Admin_Permissions : Permissions.Admin_Settings,
                "Contracts" => action == "Index" || action == "Details" ? Permissions.Contracts_View : Permissions.Contracts_Manage,
                "Solutions" => action == "Index" || action == "Details" ? Permissions.Solutions_View : Permissions.Solutions_Manage,
                _ => string.Empty
            };
        }
    }
}
