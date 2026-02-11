using System.Linq;
using Microsoft.AspNetCore.Mvc;
using ServiceCore.Data;
using ServiceCore.Models;
using Microsoft.EntityFrameworkCore;

namespace ServiceCore.Controllers
{
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class AssetsController : Controller
    {
        private readonly ServiceCoreDbContext _db;

        public AssetsController(ServiceCoreDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index(string search, int? categoryId, int page = 1, int pageSize = 20)
        {
            var query = _db.Assets.Include(a => a.Category).Include(a => a.User).AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(a => (a.Name ?? "").Contains(search) || (a.Tag ?? "").Contains(search));
            if (categoryId.HasValue)
                query = query.Where(a => a.CategoryId == categoryId);

            var total = query.Count();
            var items = query.OrderBy(a => a.Name)
                             .Skip((page - 1) * pageSize)
                             .Take(pageSize)
                             .ToList();

            ViewBag.Categories = await _db.AssetCategories.ToListAsync();
            ViewData["TotalCount"] = total;
            ViewData["Page"] = page;
            ViewData["PageSize"] = pageSize;
            ViewData["Search"] = search;
            ViewData["CategoryId"] = categoryId;

            return View(items);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Asset asset)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = await _db.AssetCategories.ToListAsync();
                ViewBag.Departments = await _db.Departments.ToListAsync();
                ViewBag.Users = await _db.Users.ToListAsync();
                return View(asset);
            }
            _db.Assets.Add(asset);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var asset = await _db.Assets.FirstOrDefaultAsync(a => a.Id == id);
            if (asset == null) return NotFound();
            
            ViewBag.Categories = await _db.AssetCategories.ToListAsync();
            ViewBag.Departments = await _db.Departments.ToListAsync();
            ViewBag.Users = await _db.Users.ToListAsync();
            return View(asset);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Asset asset)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = await _db.AssetCategories.ToListAsync();
                ViewBag.Departments = await _db.Departments.ToListAsync();
                ViewBag.Users = await _db.Users.ToListAsync();
                return View(asset);
            }
            _db.Assets.Update(asset);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var asset = await _db.Assets
                .Include(a => a.Category)
                .Include(a => a.User)
                .Include(a => a.Assignments.OrderByDescending(asg => asg.AssignedDate)).ThenInclude(asg => asg.User)
                .Include(a => a.Maintenances.OrderByDescending(m => m.MaintenanceDate))
                .FirstOrDefaultAsync(a => a.Id == id);
            
            if (asset == null) return NotFound();

            ViewBag.Users = await _db.Users.OrderBy(u => u.Name).ToListAsync();
            
            return View(asset);
        }

        [HttpPost]
        public async Task<IActionResult> Assign(int assetId, int userId, string notes)
        {
            var asset = await _db.Assets.FindAsync(assetId);
            if (asset == null) return NotFound();

            asset.UserId = userId;
            asset.Status = "In Use";

            var assignment = new AssetAssignment
            {
                AssetId = assetId,
                UserId = userId,
                AssignedDate = DateTime.UtcNow,
                Notes = notes
            };

            var history = new AssetHistory
            {
                AssetId = assetId,
                Action = "Assignment",
                Notes = $"Assigned to user ID {userId}. {notes}",
                StatusFrom = "Available",
                StatusTo = "In Use",
                ChangedBy = User.Identity?.Name
            };

            _db.AssetAssignments.Add(assignment);
            _db.AssetHistories.Add(history);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = assetId });
        }

        [HttpPost]
        public async Task<IActionResult> LogMaintenance(int assetId, string type, string description, decimal cost)
        {
            var maintenance = new AssetMaintenance
            {
                AssetId = assetId,
                MaintenanceDate = DateTime.UtcNow,
                Type = type,
                Description = description,
                Cost = cost,
                PerformedBy = User.Identity?.Name
            };

            var history = new AssetHistory
            {
                AssetId = assetId,
                Action = "Maintenance",
                Notes = $"Logged {type} maintenance: {description}. Cost: {cost:C}",
                ChangedBy = User.Identity?.Name
            };

            _db.AssetMaintenances.Add(maintenance);
            _db.AssetHistories.Add(history);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = assetId });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var asset = await _db.Assets.FirstOrDefaultAsync(a => a.Id == id);
            if (asset == null) return NotFound();
            _db.Assets.Remove(asset);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
