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
                "Home" => Permissions.Dashboard_View,
                "Tickets" => action == "Index" ? string.Empty :
                             action == "Details" ? Permissions.Tickets_View : 
                             action == "Create" ? Permissions.Tickets_Create :
                             action == "Edit" ? Permissions.Tickets_Edit :
                             action == "Delete" ? Permissions.Tickets_Delete : Permissions.Tickets_Manage,
                "TicketsManager" => Permissions.Tickets_Manage,
                "Projects" => action == "Index" ? Permissions.Projects_View : Permissions.Projects_Manage,
                "Kanban" => Permissions.Kanban_View,
                "Users" => action == "Index" || action == "Details" ? Permissions.Users_View : 
                           action == "Create" || action == "Register" ? Permissions.Users_Create :
                           action == "Edit" ? Permissions.Users_Edit :
                           action == "Delete" ? Permissions.Users_Delete : Permissions.Users_Manage,
                "UsersManager" => Permissions.Users_Manage,
                "Reports" => Permissions.Reports_View,
                "Assets" => action == "Index" || action == "Details" ? Permissions.Assets_View : 
                            action == "Create" ? Permissions.Assets_Create :
                            action == "Edit" ? Permissions.Assets_Edit :
                            action == "Delete" ? Permissions.Assets_Delete : Permissions.Assets_Manage,
                "Approvals" => Permissions.Approvals_View,
                "Admin" => action == "ManagePermissions" ? Permissions.Admin_Permissions : Permissions.Admin_Settings,
                "Contracts" => action == "Index" || action == "Details" ? Permissions.Contracts_View : 
                               action == "Create" ? Permissions.Contracts_Create :
                               action == "Edit" ? Permissions.Contracts_Edit :
                               action == "Delete" ? Permissions.Contracts_Delete : Permissions.Contracts_Manage,
                "Solutions" => action == "Index" || action == "Details" ? Permissions.Solutions_View : 
                               action == "Create" ? Permissions.Solutions_Create :
                               action == "Edit" ? Permissions.Solutions_Edit :
                               action == "Delete" ? Permissions.Solutions_Delete : Permissions.Solutions_Manage,
                "Help" => Permissions.Help_Usage,
                "Tasks" => action == "Index" || action == "Details" ? Permissions.Tasks_View : 
                           action == "Create" ? Permissions.Tasks_Create :
                           action == "Edit" ? Permissions.Tasks_Edit :
                           action == "Delete" ? Permissions.Tasks_Delete : Permissions.Tasks_Manage,
                "ProjectsManager" => Permissions.Projects_Manage,
                "Settings" => Permissions.Admin_Settings,
                _ => string.Empty
            };
        }
    }
}
