using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceCore.Data;
using ServiceCore.Models;

namespace ServiceCore.Controllers
{
    [Authorize] // Simple authorization
    public class UsersController : Controller
    {
        private readonly ServiceCoreDbContext _context;
        private readonly ServiceCore.Services.IEmailService _emailService;

        public UsersController(ServiceCoreDbContext context, ServiceCore.Services.IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
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
                // In a real app, we would send an email here.
                // For now, just create the user shell.
                _context.Add(user);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }

        // GET: Users/Invite
        public IActionResult Invite()
        {
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
                return View();
            }

            var existingUser = await _context.Users.AnyAsync(u => u.Email == email);
            if (existingUser)
            {
                ModelState.AddModelError("Email", "User with this email already exists");
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
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();
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
                    _context.Update(user);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Users.Any(e => e.Id == user.Id)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }
    }
}
