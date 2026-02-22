using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using ServiceCore.Data;
using ServiceCore.Models;

namespace ServiceCore.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ProjectsManagerController : Controller
    {
        private readonly ServiceCoreDbContext _db;

        public ProjectsManagerController(ServiceCoreDbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            var projects = _db.Projects
                .Include(p => p.Owner)
                .ToList();
            return View(projects);
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Users = _db.Users.ToList();
            return View();
        }

        [HttpPost]
        public IActionResult Create(Project project)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Users = _db.Users.ToList();
                return View(project);
            }
            _db.Projects.Add(project);
            _db.SaveChanges();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var project = _db.Projects.FirstOrDefault(p => p.Id == id);
            if (project == null) return NotFound();
            ViewBag.Users = _db.Users.ToList();
            return View(project);
        }

        [HttpPost]
        public IActionResult Edit(Project project)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Users = _db.Users.ToList();
                return View(project);
            }
            _db.Projects.Update(project);
            _db.SaveChanges();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            var project = _db.Projects.FirstOrDefault(p => p.Id == id);
            if (project == null) return NotFound();
            _db.Projects.Remove(project);
            _db.SaveChanges();
            return RedirectToAction(nameof(Index));
        }
    }
}
