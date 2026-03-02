using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceCore.Data;
using ServiceCore.Models;
using System.Security.Claims;

namespace ServiceCore.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly ServiceCoreDbContext _db;
        private readonly IPasswordHasher<User> _passwordHasher;

        public ProfileController(ServiceCoreDbContext db, IPasswordHasher<User> passwordHasher)
        {
            _db = db;
            _passwordHasher = passwordHasher;
        }

        public async Task<IActionResult> Index()
        {
            var userId = GetUserId();
            var user = await _db.Users
                .Include(u => u.Assets)
                .Include(u => u.SubmittedTickets)
                    .ThenInclude(t => t.Status)
                .Include(u => u.AssignedTasks)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return NotFound();

            return View(user);
        }

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var userId = GetUserId();
            var user = await _db.Users.FindAsync(userId);
            if (user == null) return NotFound();

            ViewBag.Departments = _db.Departments.OrderBy(d => d.Name).Select(d => d.Name).ToList();
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(User model)
        {
            var userId = GetUserId();
            var user = await _db.Users.FindAsync(userId);
            if (user == null) return NotFound();

            // Only allow editing specific fields for security
            user.Name = model.Name;
            user.PhoneNumber = model.PhoneNumber;
            user.Department = model.Department;

            if (ModelState.IsValid)
            {
                _db.Users.Update(user);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Profile updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Departments = _db.Departments.OrderBy(d => d.Name).Select(d => d.Name).ToList();
            return View(model);
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            var userId = GetUserId();
            var user = await _db.Users.FindAsync(userId);
            if (user == null) return NotFound();

            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError("confirmPassword", "Passwords do not match.");
                return View();
            }

            // Verify current password
            var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash ?? "", currentPassword);
            if (verificationResult == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError("currentPassword", "Incorrect current password.");
                return View();
            }

            user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);
            _db.Users.Update(user);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Password changed successfully.";
            return RedirectToAction(nameof(Index));
        }

        private int GetUserId()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdStr, out var id) ? id : 0;
        }
    }
}
