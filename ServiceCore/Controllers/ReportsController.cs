using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
            // Simple aggregate stats
            var projectStats = await _context.Projects
                .Select(p => new 
                {
                    Name = p.Name,
                    TaskCount = p.Tasks.Count(),
                    CompletedTaskCount = p.Tasks.Count(t => t.Status == "Done")
                })
                .ToListAsync();

            ViewData["ProjectStats"] = projectStats;
            return View();
        }
    }
}
