using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceCore.Models;

namespace ServiceCore.Controllers
{
    public class HomeController : Controller
    {
        private readonly ServiceCore.Data.ServiceCoreDbContext _db;

        public HomeController(ServiceCore.Data.ServiceCoreDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            // Ticket Metrics
            var totalTickets = _db.Tickets.Count();
            var openTickets = _db.Tickets.Count(t => t.Status.Name != "Closed" && t.Status.Name != "Resolved");
            
            // Live SLA Compliance calculation
            var totalSlaTickets = _db.Tickets.Count(t => t.DueDate != null);
            var compliantClosed = _db.Tickets.Count(t => (t.Status.Name == "Closed" || t.Status.Name == "Resolved") && t.ResolutionDate <= t.DueDate);
            var compliantOpen = _db.Tickets.Count(t => (t.Status.Name != "Closed" && t.Status.Name != "Resolved") && DateTime.Now <= t.DueDate);
            
            double slaCompliance = totalSlaTickets > 0 ? (double)(compliantClosed + compliantOpen) / totalSlaTickets * 100 : 100;

            // Long Time (Stale) Tickets - Open tickets older than 7 days
            var staleThreshold = DateTime.Now.AddDays(-7);
            var longTimeTickets = _db.Tickets
                .Include(t => t.Status)
                .Include(t => t.Priority)
                .Include(t => t.Category)
                .Where(t => (t.Status.Name != "Closed" && t.Status.Name != "Resolved") && t.CreatedAt <= staleThreshold)
                .OrderBy(t => t.CreatedAt)
                .Take(5)
                .ToList();

            // Project Metrics
            var totalProjects = _db.Projects.Count();
            var activeProjects = _db.Projects.Count(p => p.Status == "In Progress" || p.Status == "Active");
            var totalTasks = _db.ProjectTasks.Count();
            var overdueTasks = _db.ProjectTasks.Count(t => t.Status != "Completed" && t.DueDate < DateTime.Now);

            // Problem Metrics
            var activeProblems = _db.Problems.Count(p => p.Status != "Closed" && p.Status != "Resolved");
            var knownErrors = _db.Problems.Count(p => p.Status == "Known Error");

            // Change Metrics
            var activeChanges = _db.ChangeRequests.Count(c => c.Status != "Closed" && c.Status != "Rejected");
            var emergencyChanges = _db.ChangeRequests.Count(c => c.Type == "Emergency" && (c.Status != "Closed" && c.Status != "Rejected"));

            // Assets, Contracts, Solutions Metrics
            var totalAssets = await _db.Assets.CountAsync();
            var activeContracts = await _db.Contracts.CountAsync(c => c.Status == "Active");
            var publishedSolutions = await _db.Solutions.CountAsync(s => s.Status == "Published");

            // Passing to View
            ViewBag.TotalTickets = totalTickets;
            ViewBag.OpenTickets = openTickets;
            ViewBag.SlaCompliance = slaCompliance;
            ViewBag.LongTimeTickets = longTimeTickets;
            ViewBag.TotalProjects = totalProjects;
            ViewBag.ActiveProjects = activeProjects;
            ViewBag.TotalTasks = totalTasks;
            ViewBag.OverdueTasks = overdueTasks;
            ViewBag.ActiveProblems = activeProblems;
            ViewBag.KnownErrors = knownErrors;
            ViewBag.ActiveChanges = activeChanges;
            ViewBag.EmergencyChanges = emergencyChanges;
            ViewBag.TotalAssets = totalAssets;
            ViewBag.ActiveContracts = activeContracts;
            ViewBag.PublishedSolutions = publishedSolutions;

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [HttpGet]
        public IActionResult GetLiveMetrics()
        {
            var totalTickets = _db.Tickets.Count();
            var openTickets = _db.Tickets.Count(t => t.Status.Name != "Closed" && t.Status.Name != "Resolved");
            
            var totalSlaTickets = _db.Tickets.Count(t => t.DueDate != null);
            var compliantClosed = _db.Tickets.Count(t => (t.Status.Name == "Closed" || t.Status.Name == "Resolved") && t.ResolutionDate <= t.DueDate);
            var compliantOpen = _db.Tickets.Count(t => (t.Status.Name != "Closed" && t.Status.Name != "Resolved") && DateTime.Now <= t.DueDate);
            
            double slaCompliance = totalSlaTickets > 0 ? (double)(compliantClosed + compliantOpen) / totalSlaTickets * 100 : 100;

            return Json(new {
                totalTickets = totalTickets,
                openTickets = openTickets,
                slaCompliance = slaCompliance.ToString("0.0")
            });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
