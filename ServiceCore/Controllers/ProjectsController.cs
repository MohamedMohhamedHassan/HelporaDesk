using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceCore.Data;
using ServiceCore.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

namespace ServiceCore.Controllers
{
    [Authorize]
    public class ProjectsController : Controller
    {
        private readonly ServiceCoreDbContext _context;

        public ProjectsController(ServiceCoreDbContext context)
        {
            _context = context;
        }

        // GET: Projects
        public async Task<IActionResult> Index()
        {
            var projects = await _context.Projects
                .Include(p => p.Owner)
                .Include(p => p.Tasks)
                .ToListAsync();
            return View(projects);
        }

        // GET: Projects/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var project = await _context.Projects
                .Include(p => p.Owner)
                .Include(p => p.Tasks).ThenInclude(t => t.Assignee)
                .Include(p => p.Milestones)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (project == null) return NotFound();

            return View(project);
        }

        // GET: Projects/Create
        public IActionResult Create()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            ViewData["OwnerId"] = new SelectList(_context.Users, "Id", "Name", userId);
            ViewData["TeamLeadId"] = new SelectList(_context.Users, "Id", "Name");
            ViewData["AllUsers"] = _context.Users.ToList();
            return View();
        }

        // POST: Projects/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description,Status,Priority,StartDate,EndDate,OwnerId,TeamLeadId")] Project project, int[] teamMemberIds)
        {
            if (ModelState.IsValid)
            {
                _context.Add(project);
                await _context.SaveChangesAsync(); // Save project first to generate ID
                
                // Add team members
                if (teamMemberIds != null && teamMemberIds.Length > 0)
                {
                    var teamMembers = await _context.Users.Where(u => teamMemberIds.Contains(u.Id)).ToListAsync();
                    foreach (var member in teamMembers)
                    {
                        project.TeamMembers.Add(member);
                    }
                    await _context.SaveChangesAsync();
                }
                
                // Log activity
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
                _context.ActivityLogs.Add(new ActivityLog
                {
                    Action = "Created Project",
                    Description = $"Project '{project.Name}' was created.",
                    ProjectId = project.Id,
                    UserId = userId
                });

                await _context.SaveChangesAsync(); // Save log
                return RedirectToAction(nameof(Index));
            }
            ViewData["OwnerId"] = new SelectList(_context.Users, "Id", "Name", project.OwnerId);
            ViewData["TeamLeadId"] = new SelectList(_context.Users, "Id", "Name", project.TeamLeadId);
            ViewData["AllUsers"] = _context.Users.ToList();
            return View(project);
        }

        // GET: Projects/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var project = await _context.Projects
                .Include(p => p.TeamMembers)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (project == null) return NotFound();

            ViewData["OwnerId"] = new SelectList(_context.Users, "Id", "Name", project.OwnerId);
            ViewData["TeamLeadId"] = new SelectList(_context.Users, "Id", "Name", project.TeamLeadId);
            ViewData["AllUsers"] = _context.Users.ToList();
            ViewData["SelectedTeamMembers"] = project.TeamMembers.Select(tm => tm.Id).ToArray();
            return View(project);
        }

        // POST: Projects/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,Status,Priority,StartDate,EndDate,OwnerId,TeamLeadId")] Project project, int[] teamMemberIds)
        {
            if (id != project.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Load existing project with team members
                    var existingProject = await _context.Projects
                        .Include(p => p.TeamMembers)
                        .FirstOrDefaultAsync(p => p.Id == id);
                    
                    if (existingProject == null) return NotFound();
                    
                    // Update scalar properties
                    existingProject.Name = project.Name;
                    existingProject.Description = project.Description;
                    existingProject.Status = project.Status;
                    existingProject.Priority = project.Priority;
                    existingProject.StartDate = project.StartDate;
                    existingProject.EndDate = project.EndDate;
                    existingProject.OwnerId = project.OwnerId;
                    existingProject.TeamLeadId = project.TeamLeadId;
                    
                    // Update team members
                    existingProject.TeamMembers.Clear();
                    if (teamMemberIds != null && teamMemberIds.Length > 0)
                    {
                        var teamMembers = await _context.Users.Where(u => teamMemberIds.Contains(u.Id)).ToListAsync();
                        foreach (var member in teamMembers)
                        {
                            existingProject.TeamMembers.Add(member);
                        }
                    }
                    
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProjectExists(project.Id)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["OwnerId"] = new SelectList(_context.Users, "Id", "Name", project.OwnerId);
            ViewData["TeamLeadId"] = new SelectList(_context.Users, "Id", "Name", project.TeamLeadId);
            ViewData["AllUsers"] = _context.Users.ToList();
            return View(project);
        }

        // POST: Projects/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project != null)
            {
                _context.Projects.Remove(project);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool ProjectExists(int id)
        {
            return _context.Projects.Any(e => e.Id == id);
        }
    }
}
