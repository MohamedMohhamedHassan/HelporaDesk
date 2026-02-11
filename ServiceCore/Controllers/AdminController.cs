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
            ViewBag.Categories = await _db.TicketCategories.ToListAsync();
            ViewBag.Priorities = await _db.TicketPriorities.ToListAsync();
            ViewBag.Departments = await _db.Departments.ToListAsync();
            return View();
        }

        // Categories
        [HttpPost]
        public async Task<IActionResult> UpsertCategory(int? id, string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return RedirectToAction(nameof(Index));

            if (id.HasValue && id > 0)
            {
                var cat = await _db.TicketCategories.FindAsync(id);
                if (cat != null) cat.Name = name;
            }
            else
            {
                _db.TicketCategories.Add(new TicketCategory { Name = name });
            }
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
                "Assets_View", "Assets_Manage", "Approvals_View", "Approvals_Manage"
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
    }
}
