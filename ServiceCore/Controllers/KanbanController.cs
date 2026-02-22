using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceCore.Data;
using ServiceCore.Models;
using System.Security.Claims;

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
                .Include(t => t.Project).ThenInclude(p => p.TeamMembers)
                .Include(t => t.Assignee)
                .AsQueryable();

            // Role-based visibility
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

            if (projectId.HasValue)
            {
                tasksQuery = tasksQuery.Where(t => t.ProjectId == projectId.Value);
                ViewData["ProjectId"] = projectId.Value;
            }

            if (!string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                // Non-admins see tasks assigned to them OR tasks in projects where they are owner/teamlead/member
                tasksQuery = tasksQuery.Where(t => t.AssigneeId == userId ||
                    (t.Project != null && (t.Project.OwnerId == userId || t.Project.TeamLeadId == userId || t.Project.TeamMembers.Any(tm => tm.Id == userId))));
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
