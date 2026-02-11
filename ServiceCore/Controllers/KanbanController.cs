using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceCore.Data;
using ServiceCore.Models;

namespace ServiceCore.Controllers
{
    [Authorize]
    public class KanbanController : Controller
    {
        private readonly ServiceCoreDbContext _context;

        public KanbanController(ServiceCoreDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int? projectId)
        {
            var tasksQuery = _context.ProjectTasks
                .Include(t => t.Project)
                .Include(t => t.Assignee)
                .AsQueryable();

            if (projectId.HasValue)
            {
                tasksQuery = tasksQuery.Where(t => t.ProjectId == projectId.Value);
                ViewData["ProjectId"] = projectId.Value;
            }

            var tasks = await tasksQuery.ToListAsync();
            return View(tasks);
        }

        [HttpPost]
        public async Task<IActionResult> MoveTask(int taskId, string newStatus)
        {
            var task = await _context.ProjectTasks.FindAsync(taskId);
            if (task == null) return NotFound();

            task.Status = newStatus;
            _context.Update(task);
            await _context.SaveChangesAsync();

            return Ok();
        }
    }
}
