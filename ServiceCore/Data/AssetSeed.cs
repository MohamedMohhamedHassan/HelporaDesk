using System.Linq;
using ServiceCore.Data;
using ServiceCore.Models;
using Microsoft.EntityFrameworkCore;

namespace ServiceCore.Data
{
    public static class AssetSeed
    {
        public static async Task SeedAsync(ServiceCoreDbContext context)
        {
            if (!await context.AssetCategories.AnyAsync())
            {
                context.AssetCategories.AddRange(
                    new AssetCategory { Name = "Hardware", Description = "Laptops, Desktops, Monitors, etc." },
                    new AssetCategory { Name = "Software", Description = "Licenses and Subscriptions" },
                    new AssetCategory { Name = "Furniture", Description = "Desks, Chairs, etc." },
                    new AssetCategory { Name = "Networking", Description = "Routers, Switches, Access Points" }
                );
                await context.SaveChangesAsync();
            }
        }
    }
}
