using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceCore.Data;
using ServiceCore.Models;

namespace ServiceCore.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ServiceCoreDbContext _db;

        public AdminController(ServiceCoreDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.Categories = await _db.TicketCategories
                .Include(c => c.Parent)
                .OrderBy(c => c.Name)
                .ToListAsync();
            ViewBag.Priorities = await _db.TicketPriorities.ToListAsync();
            ViewBag.Departments = await _db.Departments.ToListAsync();
            return View();
        }

        // Categories
        [HttpPost]
        public async Task<IActionResult> UpsertCategory(int? id, string name, int? parentId)
        {
            if (string.IsNullOrWhiteSpace(name)) return RedirectToAction(nameof(Index));

            if (id.HasValue && id > 0)
            {
                var cat = await _db.TicketCategories.FindAsync(id);
                if (cat != null)
                {
                    cat.Name = name;
                    cat.ParentId = parentId;
                }
            }
            else
            {
                _db.TicketCategories.Add(new TicketCategory { Name = name, ParentId = parentId });
            }
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var cat = await _db.TicketCategories.FindAsync(id);
            if (cat == null) return NotFound();

            // Check if it has children or tickets
            var hasChildren = await _db.TicketCategories.AnyAsync(c => c.ParentId == id);
            var hasTickets = await _db.Tickets.AnyAsync(t => t.CategoryId == id);

            if (hasChildren || hasTickets)
            {
                // Simple error handling for now - could be improved with Toast or similar
                return RedirectToAction(nameof(Index));
            }

            _db.TicketCategories.Remove(cat);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // Priorities
        [HttpPost]
        public async Task<IActionResult> UpsertPriority(int? id, string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return RedirectToAction(nameof(Index));

            if (id.HasValue && id > 0)
            {
                var prio = await _db.TicketPriorities.FindAsync(id);
                if (prio != null) prio.Name = name;
            }
            else
            {
                _db.TicketPriorities.Add(new TicketPriority { Name = name });
            }
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // Departments
        [HttpPost]
        public async Task<IActionResult> UpsertDepartment(int? id, string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return RedirectToAction(nameof(Index));

            if (id.HasValue && id > 0)
            {
                var dept = await _db.Departments.FindAsync(id);
                if (dept != null) dept.Name = name;
            }
            else
            {
                _db.Departments.Add(new Department { Name = name });
            }
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // Role Management
        public async Task<IActionResult> ManageRoles()
        {
            var roles = await _db.Roles.ToListAsync();
            
            // Seed defaults if empty
            if (!roles.Any())
            {
                var defaults = new[] { 
                    new Role { Name = "Admin", Description = "Full system access" },
                    new Role { Name = "Agent", Description = "Manage tickets and assets" },
                    new Role { Name = "Technical", Description = "Technical support and infrastructure" },
                    new Role { Name = "Member", Description = "Organizational representative" },
                    new Role { Name = "User", Description = "Standard requester access" }
                };
                _db.Roles.AddRange(defaults);
                await _db.SaveChangesAsync();
                roles = await _db.Roles.ToListAsync();
            }

            return View(roles);
        }

        [HttpPost]
        public async Task<IActionResult> CreateRole(string name, string description)
        {
            if (string.IsNullOrWhiteSpace(name)) return BadRequest();

            var role = new Role { Name = name, Description = description };
            _db.Roles.Add(role);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(ManageRoles));
        }

        [HttpPost]
        public async Task<IActionResult> UpdateRole(int id, string name, string description)
        {
            var role = await _db.Roles.FindAsync(id);
            if (role == null) return NotFound();

            role.Name = name;
            role.Description = description;
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(ManageRoles));
        }

        // Permission Management
        public async Task<IActionResult> ManagePermissions(string? selectedRole = null)
        {
            var roles = await _db.Roles.Select(r => r.Name).ToListAsync();
            if (!roles.Any())
            {
                // Ensure defaults exist
                var defaults = new[] { 
                    new Role { Name = "Admin", Description = "Full system access" },
                    new Role { Name = "Agent", Description = "Manage tickets and assets" },
                    new Role { Name = "Technical", Description = "Technical support and infrastructure" },
                    new Role { Name = "Member", Description = "Organizational representative" },
                    new Role { Name = "User", Description = "Standard requester access" }
                };
                _db.Roles.AddRange(defaults);
                await _db.SaveChangesAsync();
                roles = await _db.Roles.Select(r => r.Name).ToListAsync();
            }

            selectedRole ??= roles.First();

            var permissions = await _db.RolePermissions.ToListAsync();
            
            var features = new[] { 
                "Dashboard", "Tickets_View", "Tickets_Create", "Tickets_Edit", "Tickets_Delete", "Tickets_Manager",
                "Projects_View", "Projects_Manage", "Tasks_View", "Tasks_Manage", "Kanban_Board",
                "Users_View", "Users_Manage", "Reports_View", "Admin_Metadata", "Admin_Settings", "Admin_Permissions",
                "Assets_View", "Assets_Manage", "Approvals_View", "Approvals_Manage",
                "Solutions_View", "Solutions_Create", "Solutions_Manage", "Solutions_Topics",
                "Contracts_View", "Contracts_Create", "Contracts_Manage", "Contracts_Approvals", "Vendors_Manage"
            };

            bool changed = false;
            foreach (var feature in features)
            {
                if (!permissions.Any(p => p.FeatureKey == feature && p.RoleName == selectedRole))
                {
                    _db.RolePermissions.Add(new RolePermission 
                    { 
                        FeatureKey = feature, 
                        RoleName = selectedRole, 
                        IsAllowed = (selectedRole == "Admin")
                    });
                    changed = true;
                }
            }

            if (changed)
            {
                await _db.SaveChangesAsync();
                permissions = await _db.RolePermissions.ToListAsync();
            }

            ViewBag.Roles = roles;
            ViewBag.SelectedRole = selectedRole;
            ViewBag.Features = features;
            return View(permissions.Where(p => p.RoleName == selectedRole).ToList());
        }

        [HttpPost]
        public async Task<IActionResult> UpdateBulkPermissions(string role, [FromBody] Dictionary<string, bool> permissions)
        {
            if (string.IsNullOrEmpty(role) || permissions == null) 
                return Json(new { success = false, message = "Invalid data" });

            var existingPerms = await _db.RolePermissions.Where(p => p.RoleName == role).ToListAsync();

            foreach (var pair in permissions)
            {
                var perm = existingPerms.FirstOrDefault(p => p.FeatureKey == pair.Key);
                if (perm != null)
                {
                    perm.IsAllowed = pair.Value;
                }
                else
                {
                    _db.RolePermissions.Add(new RolePermission 
                    { 
                        RoleName = role, 
                        FeatureKey = pair.Key, 
                        IsAllowed = pair.Value 
                    });
                }
            }

            await _db.SaveChangesAsync();
            return Json(new { success = true });
        }

        // Solution Topics Management
        public async Task<IActionResult> ManageTopics()
        {
            var topics = await _db.SolutionTopics
                .Include(t => t.Parent)
                .OrderBy(t => t.Name)
                .ToListAsync();

            // Seed default topics if empty
            if (!topics.Any())
            {
                var defaults = new[]
                {
                    new SolutionTopic { Name = "General", Description = "General solutions and guides" },
                    new SolutionTopic { Name = "Hardware", Description = "Hardware-related issues" },
                    new SolutionTopic { Name = "Software", Description = "Software and application issues" },
                    new SolutionTopic { Name = "Network", Description = "Network and connectivity problems" },
                    new SolutionTopic { Name = "Printers", Description = "Printer setup and troubleshooting" },
                    new SolutionTopic { Name = "Email", Description = "Email configuration and issues" },
                    new SolutionTopic { Name = "Security", Description = "Security and access control" },
                    new SolutionTopic { Name = "Operating System", Description = "OS-specific solutions" }
                };
                _db.SolutionTopics.AddRange(defaults);
                await _db.SaveChangesAsync();
                topics = await _db.SolutionTopics.ToListAsync();
            }

            return View(topics);
        }

        [HttpPost]
        public async Task<IActionResult> UpsertTopic(int? id, string name, string? description, int? parentId)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Json(new { success = false, message = "Name is required" });

            if (id.HasValue && id > 0)
            {
                var topic = await _db.SolutionTopics.FindAsync(id);
                if (topic != null)
                {
                    topic.Name = name;
                    topic.Description = description;
                    topic.ParentId = parentId;
                }
            }
            else
            {
                _db.SolutionTopics.Add(new SolutionTopic
                {
                    Name = name,
                    Description = description,
                    ParentId = parentId
                });
            }

            await _db.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteTopic(int id)
        {
            var topic = await _db.SolutionTopics.FindAsync(id);
            if (topic == null)
                return Json(new { success = false, message = "Topic not found" });

            // Check if topic has solutions
            var hasSolutions = await _db.Solutions.AnyAsync(s => s.TopicId == id);
            if (hasSolutions)
                return Json(new { success = false, message = "Cannot delete topic with existing solutions" });

            _db.SolutionTopics.Remove(topic);
            await _db.SaveChangesAsync();
            return Json(new { success = true });
        }
    }
}
