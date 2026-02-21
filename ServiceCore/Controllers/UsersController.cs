using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceCore.Data;
using ServiceCore.Models;
using System;

namespace ServiceCore.Controllers
{
    [Authorize] // Simple authorization
    public class UsersController : Controller
    {
        private readonly ServiceCoreDbContext _context;
        private readonly ServiceCore.Services.IEmailService _emailService;
        private readonly IPasswordHasher<User> _passwordHasher;

        public UsersController(ServiceCoreDbContext context, ServiceCore.Services.IEmailService emailService, IPasswordHasher<User> passwordHasher)
        {
            _context = context;
            _emailService = emailService;
            _passwordHasher = passwordHasher;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.Users.ToListAsync());
        }

        // GET: Users/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Users/Create (Simulated Invite)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Email,Role,Department")] User user)
        {
            if (ModelState.IsValid)
            {
                // Create a user shell. Hash a temporary password so we never store plain text.
                user.IsActive = true;
                var tempPassword = GenerateTemporaryPassword();
                user.PasswordHash = _passwordHasher.HashPassword(user, tempPassword);

                _context.Add(user);
                await _context.SaveChangesAsync();
                // In a real app we'd email the temp password or invite link.
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }

        public async Task<IActionResult> Invite()
        {
            await PopulateDropdowns();
            return View();
        }

        // POST: Users/Invite
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Invite(string email, string role, string department)
        {
            if (string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError("Email", "Email is required");
                await PopulateDropdowns(role, department);
                return View();
            }

            var existingUser = await _context.Users.AnyAsync(u => u.Email == email);
            if (existingUser)
            {
                ModelState.AddModelError("Email", "User with this email already exists");
                await PopulateDropdowns(role, department);
                return View();
            }

            var token = Guid.NewGuid().ToString();
            var newUser = new User
            {
                Name = "Pending User", // Placeholder until they complete registration
                Email = email,
                Role = role,
                Department = department,
                InviteToken = token,
                IsActive = false // Not active until registration complete
            };

            // No password yet for invited users; they'll set it on completion.

            _context.Add(newUser);
            await _context.SaveChangesAsync();

            // Send notification email (Mock)
            var inviteLink = Url.Action("CompleteRegistration", "Account", new { token }, Request.Scheme);
            await _emailService.SendEmailAsync(
                email,
                "You're invited to ServiceCore!",
                $"Welcome! Click the link below to complete your registration:\n{inviteLink}");

            return RedirectToAction(nameof(Index));
        }

        // GET: Users/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            await PopulateDropdowns(user.Role, user.Department);
            return View(user);
        }

        // POST: Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Email,Role,Department,PhoneNumber,IsActive")] User user)
        {
            if (id != user.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var existing = await _context.Users.FindAsync(user.Id);
                    if (existing == null) return NotFound();

                    existing.Name = user.Name;
                    existing.Email = user.Email;
                    existing.Role = user.Role;
                    existing.Department = user.Department;
                    existing.PhoneNumber = user.PhoneNumber;
                    existing.IsActive = user.IsActive;

                    // Preserve existing.PasswordHash, existing.SecurityStamp, existing.InviteToken, etc.
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Users.Any(e => e.Id == user.Id)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }

            // repopulate for the view when returning due to validation errors
            await PopulateDropdowns(user.Role, user.Department);
            return View(user);
        }

        private async Task PopulateDropdowns(string? selectedRole = null, string? selectedDept = null)
        {
            ViewBag.Roles = await _context.Roles.Select(r => r.Name).ToListAsync();
            ViewBag.Departments = await _context.Departments.Select(d => d.Name).ToListAsync();
            ViewBag.SelectedRole = selectedRole;
            ViewBag.SelectedDepartment = selectedDept;
        }

        // Helper method added to fix compile error
        private string GenerateTemporaryPassword()
        {
            // Simple robust temp password for now
            return Guid.NewGuid().ToString("N").Substring(0, 8) + "Temp!1";
        }
    }
}