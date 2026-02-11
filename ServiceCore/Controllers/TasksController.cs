using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ServiceCore.Data;
using ServiceCore.Models;
using System.Security.Claims;

namespace ServiceCore.Controllers
{
    [Authorize]
    public class TasksController : Controller
    {
        private readonly ServiceCoreDbContext _context;

        public TasksController(ServiceCoreDbContext context)
        {
            _context = context;
        }

        // GET: Tasks
        public async Task<IActionResult> Index(int? projectId)
        {
            var tasksQuery = _context.ProjectTasks
                .Include(t => t.Project)
                .Include(t => t.Assignee)
                .Include(t => t.Milestone)
                .AsQueryable();

            if (projectId.HasValue)
            {
                tasksQuery = tasksQuery.Where(t => t.ProjectId == projectId.Value);
                ViewData["ProjectId"] = projectId.Value;
            }

            return View(await tasksQuery.ToListAsync());
        }

        // GET: Tasks/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var task = await _context.ProjectTasks
                .Include(t => t.Project)
                .Include(t => t.Assignee)
                .Include(t => t.Milestone)
                .Include(t => t.Comments).ThenInclude(c => c.User)
                .Include(t => t.Attachments) // Added Include
                .FirstOrDefaultAsync(m => m.Id == id);

            if (task == null) return NotFound();

            return View(task);
        }

        // GET: Tasks/Create
        public IActionResult Create(int? projectId)
        {
            ViewData["ProjectId"] = new SelectList(_context.Projects, "Id", "Name", projectId);
            ViewData["AssigneeId"] = new SelectList(_context.Users, "Id", "Name");
            
            // Get team members for the project if specified
            if (projectId.HasValue)
            {
               var project = _context.Projects.Include(p => p.TeamMembers).FirstOrDefault(p => p.Id == projectId);
               ViewData["MilestoneId"] = new SelectList(_context.Milestones.Where(m => m.ProjectId == projectId), "Id", "Title");
               ViewData["AvailableAssignees"] = project?.TeamMembers.ToList() ?? _context.Users.ToList();
            }
            else
            {
               ViewData["MilestoneId"] = new SelectList(_context.Milestones, "Id", "Title");
               ViewData["AvailableAssignees"] = _context.Users.ToList();
            }
            
            return View(new ProjectTask { ProjectId = projectId.GetValueOrDefault() });
        }

        // POST: Tasks/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Description,Status,Priority,DueDate,ProjectId,AssigneeId,MilestoneId")] ProjectTask task, int[] assigneeIds)
        {
            if (ModelState.IsValid)
            {
                _context.Add(task);
                await _context.SaveChangesAsync();
                
                // Add assignees
                if (assigneeIds != null && assigneeIds.Length > 0)
                {
                    var assignees = await _context.Users.Where(u => assigneeIds.Contains(u.Id)).ToListAsync();
                    foreach (var assignee in assignees)
                    {
                        task.Assignees.Add(assignee);
                    }
                    await _context.SaveChangesAsync();
                }
                
                return RedirectToAction(nameof(Index), new { projectId = task.ProjectId });
            }
            return View(task);
        }

        // GET: Tasks/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var task = await _context.ProjectTasks
                .Include(t => t.Assignees)
                .FirstOrDefaultAsync(t => t.Id == id);
            if (task == null) return NotFound();
            
            ViewData["ProjectId"] = new SelectList(_context.Projects, "Id", "Name", task.ProjectId);
            ViewData["AssigneeId"] = new SelectList(_context.Users, "Id", "Name", task.AssigneeId);
            ViewData["MilestoneId"] = new SelectList(_context.Milestones.Where(m => m.ProjectId == task.ProjectId), "Id", "Title", task.MilestoneId);
            
            // Get available assignees from project team
            var project = await _context.Projects.Include(p => p.TeamMembers).FirstOrDefaultAsync(p => p.Id == task.ProjectId);
            ViewData["AvailableAssignees"] = project?.TeamMembers.ToList() ?? _context.Users.ToList();
            ViewData["SelectedAssignees"] = task.Assignees.Select(a => a.Id).ToArray();
            
            return View(task);
        }

        // POST: Tasks/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,Status,Priority,DueDate,ProjectId,AssigneeId,MilestoneId")] ProjectTask task, int[] assigneeIds)
        {
            if (id != task.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Load existing task with assignees
                    var existingTask = await _context.ProjectTasks
                        .Include(t => t.Assignees)
                        .FirstOrDefaultAsync(t => t.Id == id);
                    
                    if (existingTask == null) return NotFound();
                    
                    // Update scalar properties
                    existingTask.Title = task.Title;
                    existingTask.Description = task.Description;
                    existingTask.Status = task.Status;
                    existingTask.Priority = task.Priority;
                    existingTask.DueDate = task.DueDate;
                    existingTask.ProjectId = task.ProjectId;
                    existingTask.AssigneeId = task.AssigneeId;
                    existingTask.MilestoneId = task.MilestoneId;
                    
                    // Update assignees
                    existingTask.Assignees.Clear();
                    if (assigneeIds != null && assigneeIds.Length > 0)
                    {
                        var assignees = await _context.Users.Where(u => assigneeIds.Contains(u.Id)).ToListAsync();
                        foreach (var assignee in assignees)
                        {
                            existingTask.Assignees.Add(assignee);
                        }
                    }
                    
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TaskExists(task.Id)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index), new { projectId = task.ProjectId });
            }
            return View(task);
        }

        // GET: Tasks/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var task = await _context.ProjectTasks
                .Include(t => t.Project)
                .Include(t => t.Assignee)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (task == null) return NotFound();

            return View(task);
        }

        // POST: Tasks/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var task = await _context.ProjectTasks.FindAsync(id);
            if (task != null)
            {
                _context.ProjectTasks.Remove(task);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index), new { projectId = task?.ProjectId });
        }

        // POST: Tasks/AddComment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int taskId, string content)
        {
            var task = await _context.ProjectTasks.FindAsync(taskId);
            if (task == null) return NotFound();

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

            var comment = new Comment
            {
                ProjectTaskId = taskId,
                Content = content,
                UserId = userId,
                CreatedAt = DateTime.Now
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = taskId });
        }

        // POST: Tasks/UploadAttachment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAttachment(int taskId, IFormFile file)
        {
            var task = await _context.ProjectTasks.FindAsync(taskId);
            if (task == null) return NotFound();

            if (file != null && file.Length > 0)
            {
                // Ensure directory exists
                var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

                // Generate unique filename
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                var filePath = Path.Combine(uploadPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
                var attachment = new Attachment
                {
                    FileName = file.FileName,
                    FilePath = "/uploads/" + fileName,
                    UploadedAt = DateTime.Now,
                    ProjectTaskId = taskId,
                    UserId = userId
                };

                _context.Attachments.Add(attachment);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Details), new { id = taskId });
        }

        private bool TaskExists(int id)
        {
            return _context.ProjectTasks.Any(e => e.Id == id);
        }
    }
}
