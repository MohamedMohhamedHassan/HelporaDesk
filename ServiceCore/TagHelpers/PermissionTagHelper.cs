using Microsoft.AspNetCore.Razor.TagHelpers;
using ServiceCore.Services;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ServiceCore.TagHelpers
{
    [HtmlTargetElement("secure-content")]
    [HtmlTargetElement(Attributes = "asp-authorize")]
    public class PermissionTagHelper : TagHelper
    {
        private readonly IPermissionService _permissionService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public PermissionTagHelper(IPermissionService permissionService, IHttpContextAccessor httpContextAccessor)
        {
            _permissionService = permissionService;
            _httpContextAccessor = httpContextAccessor;
        }

        [HtmlAttributeName("asp-authorize")]
        public string Permission { get; set; }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            if (string.IsNullOrEmpty(Permission))
            {
                return; // No permission specified, show content
            }

            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null || !user.Identity.IsAuthenticated)
            {
                output.SuppressOutput();
                return;
            }

            var hasPermission = await _permissionService.HasPermissionAsync(user, Permission);
            
            if (!hasPermission)
            {
                output.SuppressOutput();
            }
            
            // If it's a secure-content tag, remove the tag itself but keep content (if allowed)
            if (output.TagName == "secure-content")
            {
                output.TagName = null;
            }
        }
    }
}
