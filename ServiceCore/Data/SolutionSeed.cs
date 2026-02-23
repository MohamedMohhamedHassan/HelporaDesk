using System.Linq;
using Microsoft.EntityFrameworkCore;
using ServiceCore.Models;

namespace ServiceCore.Data
{
    public static class SolutionSeed
    {
        public static async Task SeedAsync(ServiceCoreDbContext context)
        {
            if (!await context.SolutionTopics.AnyAsync())
            {
                var topics = new List<SolutionTopic>
                {
                    new SolutionTopic { Name = "General", Description = "Common issues and general guidance", Icon = "bi-info-circle" },
                    new SolutionTopic { Name = "Hardware", Description = "Laptops, Printers, and peripheral issues", Icon = "bi-laptop" },
                    new SolutionTopic { Name = "Software", Description = "OS, Office 365, and application errors", Icon = "bi-app-indicator" },
                    new SolutionTopic { Name = "Networking", Description = "Connectivity, VPN, and Wi-Fi issues", Icon = "bi-wifi" },
                    new SolutionTopic { Name = "Security", Description = "Access requests, passwords, and security alerts", Icon = "bi-shield-lock" }
                };

                context.SolutionTopics.AddRange(topics);
                await context.SaveChangesAsync();
            }
        }
    }
}
