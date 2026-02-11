using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using ServiceCore.Data;
using ServiceCore.Models;

namespace ServiceCore.Controllers
{
    public class UsersManagerController : Controller
    {
        private readonly ServiceCoreDbContext _db;

        public UsersManagerController(ServiceCoreDbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            var users = _db.Users.OrderBy(u => u.Name).ToList();
            return View(users);
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Roles = new[] { "Admin", "Agent", "Technical", "Member", "User" };
            ViewBag.Departments = _db.Departments.Select(d => d.Name).ToList();
            return View();
        }

        [HttpPost]
        public IActionResult Create(User user, string initialPassword)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Roles = new[] { "Admin", "Agent", "Technical", "Member", "User" };
                ViewBag.Departments = _db.Departments.Select(d => d.Name).ToList();
                return View(user);
            }

            if (!string.IsNullOrWhiteSpace(initialPassword))
            {
                user.PasswordHash = ServiceCore.Models.User.HashPassword(initialPassword);
            }

            _db.Users.Add(user);
            _db.SaveChanges();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var user = _db.Users.FirstOrDefault(u => u.Id == id);
            if (user == null) return NotFound();
            
            ViewBag.Roles = new[] { "Admin", "Agent", "Technical", "Member", "User" };
            ViewBag.Departments = _db.Departments.Select(d => d.Name).ToList();
            
            return View(user);
        }

        [HttpPost]
        public IActionResult Edit(User user)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Roles = new[] { "Admin", "Agent", "Technical", "Member", "User" };
                ViewBag.Departments = _db.Departments.Select(d => d.Name).ToList();
                return View(user);
            }
            _db.Users.Update(user);
            _db.SaveChanges();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult ChangePassword(int id)
        {
            var user = _db.Users.FirstOrDefault(u => u.Id == id);
            if (user == null) return NotFound();
            return View(user);
        }

        [HttpPost]
        public IActionResult ChangePassword(int id, string newPassword)
        {
            var user = _db.Users.FirstOrDefault(u => u.Id == id);
            if (user == null) return NotFound();

            if (string.IsNullOrWhiteSpace(newPassword))
            {
                ModelState.AddModelError("", "Password cannot be empty");
                return View(user);
            }

            user.PasswordHash = ServiceCore.Models.User.HashPassword(newPassword);
            _db.Users.Update(user);
            _db.SaveChanges();
            
            TempData["Success"] = $"Password for {user.Name} updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            var user = _db.Users.FirstOrDefault(u => u.Id == id);
            if (user == null) return NotFound();
            _db.Users.Remove(user);
            _db.SaveChanges();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Export()
        {
            var users = _db.Users.OrderBy(u => u.Id).ToList();
            var sb = new StringBuilder();
            sb.AppendLine("Id,Name,Role,Department");
            foreach (var u in users)
            {
                var name = (u.Name ?? string.Empty).Replace("\"", "\"\"");
                var role = (u.Role ?? string.Empty).Replace("\"", "\"\"");
                var dept = (u.Department ?? string.Empty).Replace("\"", "\"\"");
                sb.AppendLine($"{u.Id},\"{name}\",\"{role}\",\"{dept}\"");
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv", "users.csv");
        }
    }
}
