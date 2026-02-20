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

        public IActionResult Index()
        {
            // Ticket Metrics
            var totalTickets = _db.Tickets.Count();
            var openTickets = _db.Tickets.Count(t => t.Status.Name != "Closed" && t.Status.Name != "Resolved");
            
            // SLA Compliance calculation (Tickets resolved/closed before due date)
            var closedTickets = _db.Tickets.Where(t => t.Status.Name == "Closed" || t.Status.Name == "Resolved");
            var compliantTickets = closedTickets.Count(t => t.ResolutionDate <= t.DueDate);
            double slaCompliance = closedTickets.Any() ? (double)compliantTickets / closedTickets.Count() * 100 : 100;

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

            // Passing to View
            ViewBag.TotalTickets = totalTickets;
            ViewBag.OpenTickets = openTickets;
            ViewBag.SlaCompliance = slaCompliance;
            ViewBag.LongTimeTickets = longTimeTickets;
            ViewBag.TotalProjects = totalProjects;
            ViewBag.ActiveProjects = activeProjects;
            ViewBag.TotalTasks = totalTasks;
            ViewBag.OverdueTasks = overdueTasks;

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
