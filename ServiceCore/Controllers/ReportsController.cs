using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using ServiceCore.Data;
using ServiceCore.Models;

namespace ServiceCore.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly ServiceCoreDbContext _context;

        public ReportsController(ServiceCoreDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var now = DateTime.Now;

            // 1. Ticket Stats & Aging
            var tickets = await _context.Tickets.Include(t => t.Status).Include(t => t.Priority).ToListAsync();
            var ticketStats = new
            {
                Total = tickets.Count,
                ByStatus = tickets.GroupBy(t => t.Status.Name).Select(g => new { Name = g.Key, Count = g.Count() }).ToList(),
                ByPriority = tickets.GroupBy(t => t.Priority.Name).Select(g => new { Name = g.Key, Count = g.Count() }).ToList(),
                ResolvedLast30Days = tickets.Count(t => t.Status.Name == "Closed" && t.ResolutionDate >= now.AddDays(-30)),
                // Aging: 0-24h, 1-3d, 3-7d, 7d+
                Aging = new {
                    Day1 = tickets.Count(t => t.Status.Name != "Closed" && (now - t.CreatedAt).TotalHours <= 24),
                    Day3 = tickets.Count(t => t.Status.Name != "Closed" && (now - t.CreatedAt).TotalDays > 1 && (now - t.CreatedAt).TotalDays <= 3),
                    Day7 = tickets.Count(t => t.Status.Name != "Closed" && (now - t.CreatedAt).TotalDays > 3 && (now - t.CreatedAt).TotalDays <= 7),
                    Older = tickets.Count(t => t.Status.Name != "Closed" && (now - t.CreatedAt).TotalDays > 7)
                }
            };

            // 2. Project Stats & Velocity (Tickets resolved in last 7 days)
            var last7Days = Enumerable.Range(0, 7).Select(i => now.Date.AddDays(-i)).OrderBy(d => d).ToList();
            var velocity = last7Days.Select(date => new {
                Date = date.ToString("MMM dd"),
                Count = tickets.Count(t => t.Status.Name == "Closed" && t.ResolutionDate?.Date == date)
            }).ToList();

            var projects = await _context.Projects.Include(p => p.Tasks).ToListAsync();
            var projectStats = projects.Select(p => new {
                Name = p.Name,
                Status = p.Status,
                TaskCount = p.Tasks?.Count ?? 0,
                CompletedTaskCount = p.Tasks?.Count(t => t.Status == "Done") ?? 0
            }).ToList();

            // 3. Asset Stats
            var assetStats = new
            {
                TotalCount = await _context.Assets.CountAsync(),
                TotalValue = await _context.Assets.SumAsync(a => (double?)a.PurchaseCost) ?? 0,
                ByCategory = await _context.Assets.GroupBy(a => a.Category.Name).Select(g => new { Name = g.Key, Count = g.Count() }).ToListAsync(),
                MaintenanceCount = await _context.Assets.CountAsync(a => a.Status == "Under Maintenance")
            };

            // 4. Contract Stats
            var contractStats = new
            {
                TotalValue = await _context.Contracts.SumAsync(c => (double?)c.Value) ?? 0,
                ByStatus = await _context.Contracts.GroupBy(c => c.Status).Select(g => new { Name = g.Key, Count = g.Count() }).ToListAsync(),
                UpcomingExpirations = await _context.Contracts.CountAsync(c => c.EndDate <= now.AddDays(30) && c.EndDate >= now)
            };

            // 5. Workload (Top Users)
            var userWorkload = await _context.Users
                .Select(u => new
                {
                    UserName = u.Name,
                    TicketCount = u.AssignedTickets.Count(t => t.Status.Name != "Closed"),
                    TaskCount = u.AssignedTasks.Count(t => t.Status != "Done")
                })
                .OrderByDescending(u => u.TicketCount + u.TaskCount)
                .Take(5)
                .ToListAsync();

            // 6. Solutions Stats (Knowledge Base)
            var solutionStats = new
            {
                Total = await _context.Solutions.CountAsync(),
                Published = await _context.Solutions.CountAsync(s => s.Status == "Published"),
                TotalViews = await _context.Solutions.SumAsync(s => s.Views),
                TopTopics = await _context.Solutions.Include(s => s.Topic)
                    .GroupBy(s => s.Topic.Name)
                    .Select(g => new { Name = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(3)
                    .ToListAsync()
            };

            // 7. System Health Score (Modern Metric)
            // Logic: 
            // - Tickets: 100 - (OpenTickets * 2) [max 40%]
            // - Projects: (DoneTasks / TotalTasks * 100) [max 30%]
            // - Assets: (OperationalAssets / TotalAssets * 100) [max 30%]
            int openTickets = tickets.Count(t => t.Status.Name != "Closed");
            double ticketScore = Math.Max(0, 100 - (openTickets * 5)) * 0.4;
            
            var allTasks = await _context.ProjectTasks.CountAsync();
            var doneTasks = await _context.ProjectTasks.CountAsync(t => t.Status == "Done");
            double projectScore = (allTasks > 0 ? (double)doneTasks / allTasks * 100 : 100) * 0.3;

            var totalAssets = await _context.Assets.CountAsync();
            var brokenAssets = await _context.Assets.CountAsync(a => a.Status == "Under Maintenance" || a.Status == "Retired");
            double assetScore = (totalAssets > 0 ? (double)(totalAssets - brokenAssets) / totalAssets * 100 : 100) * 0.3;

            int healthScore = (int)(ticketScore + projectScore + assetScore);

            ViewBag.TicketStats = ticketStats;
            ViewBag.ProjectStats = projectStats;
            ViewBag.AssetStats = assetStats;
            ViewBag.ContractStats = contractStats;
            ViewBag.SolutionStats = solutionStats;
            ViewBag.HealthScore = healthScore;
            ViewBag.UserWorkload = userWorkload;
            ViewBag.Velocity = velocity;

            return View();
        }

        public async Task<IActionResult> ExportCsv(
            bool includeTickets = true, 
            bool includeProjects = true, 
            bool includeAssets = true, 
            bool includeContracts = true,
            bool includeSolutions = true)
        {
            var csv = new StringBuilder();
            var now = DateTime.Now;

            // Tickets Section
            if (includeTickets)
            {
                var tickets = await _context.Tickets.Include(t => t.Status).Include(t => t.Priority).Include(t => t.Requester).ToListAsync();
                csv.AppendLine("--- TICKETS ---");
                csv.AppendLine("ID,Subject,Status,Priority,Requester,Created At,Resolved At");
                foreach (var t in tickets)
                    csv.AppendLine($"{t.Id},\"{t.Subject.Replace("\"", "'")}\",{t.Status.Name},{t.Priority.Name},{t.Requester?.Name},\"{t.CreatedAt:yyyy-MM-dd HH:mm}\",\"{t.ResolutionDate?.ToString("yyyy-MM-dd HH:mm")}\"");
                csv.AppendLine();
            }

            // Projects Section
            if (includeProjects)
            {
                var projects = await _context.Projects.Include(p => p.Owner).ToListAsync();
                csv.AppendLine("--- PROJECTS ---");
                csv.AppendLine("Name,Status,Owner,Start Date,End Date,Progress");
                foreach (var p in projects)
                {
                    var total = _context.ProjectTasks.Count(t => t.ProjectId == p.Id);
                    var done = _context.ProjectTasks.Count(t => t.ProjectId == p.Id && t.Status == "Done");
                    var progress = total > 0 ? (done * 100 / total) : 0;
                    csv.AppendLine($"\"{p.Name.Replace("\"", "'")}\",{p.Status},{p.Owner?.Name},{p.StartDate:yyyy-MM-dd},{p.EndDate:yyyy-MM-dd},{progress}%");
                }
                csv.AppendLine();
            }

            // Assets Section
            if (includeAssets)
            {
                var assets = await _context.Assets.Include(a => a.Category).ToListAsync();
                csv.AppendLine("--- ASSETS ---");
                csv.AppendLine("Tag,Name,Category,Status,Purchase Cost,Purchase Date");
                foreach (var a in assets)
                    csv.AppendLine($"{a.Tag},\"{a.Name.Replace("\"", "'")}\",{a.Category?.Name},{a.Status},{a.PurchaseCost:F2},{a.PurchaseDate:yyyy-MM-dd}");
                csv.AppendLine();
            }

            // Contracts Section (Placeholder or Summary)
            if (includeContracts)
            {
                var contracts = await _context.Contracts.Include(c => c.Vendor).Include(c => c.ContractType).ToListAsync();
                csv.AppendLine("--- CONTRACTS ---");
                csv.AppendLine("Name,Vendor,Type,Value,Expiry Date");
                foreach (var c in contracts)
                    csv.AppendLine($"\"{c.Name.Replace("\"", "'")}\",{c.Vendor?.Name},{c.ContractType?.Name},\"{c.Value:C2}\",\"{c.EndDate:yyyy-MM-dd}\"");
                csv.AppendLine();
            }

            // Solutions Section
            if (includeSolutions)
            {
                var solutions = await _context.Solutions.Include(s => s.Topic).ToListAsync();
                csv.AppendLine("--- KNOWLEDGE BASE ---");
                csv.AppendLine("Title,Topic,Status,Views,Created At");
                foreach (var s in solutions)
                    csv.AppendLine($"\"{s.Title.Replace("\"", "'")}\",{s.Topic?.Name},{s.Status},{s.Views},\"{s.CreatedAt:yyyy-MM-dd}\"");
            }

            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
            return File(buffer, "text/csv", $"ServiceCore_FullReport_{DateTime.Now:yyyyMMdd_HHmm}.csv");
        }
    }
}
