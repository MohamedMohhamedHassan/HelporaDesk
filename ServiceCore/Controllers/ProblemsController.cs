using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceCore.Data;
using ServiceCore.Models;

namespace ServiceCore.Controllers
{
    [Authorize]
    public class ProblemsController : Controller
    {
        private readonly ServiceCoreDbContext _db;

        public ProblemsController(ServiceCoreDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index(string search = "", string status = "", string priority = "")
        {
            var query = _db.Problems
                .Include(p => p.Category)
                .Include(p => p.AssignedTo)
                .Include(p => p.LinkedIncidents)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => p.Title.Contains(search) || p.Description.Contains(search));
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(p => p.Status == status);
            }

            if (!string.IsNullOrEmpty(priority))
            {
                query = query.Where(p => p.Priority == priority);
            }

            var problems = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
            
            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.Priority = priority;

            return View(problems);
        }

        public async Task<IActionResult> Details(int id)
        {
            var problem = await _db.Problems
                .Include(p => p.Category)
                .Include(p => p.AssignedTo)
                .Include(p => p.Creator)
                .Include(p => p.LinkedIncidents)
                    .ThenInclude(pi => pi.Ticket)
                        .ThenInclude(t => t.Status)
                .Include(p => p.Activities)
                    .ThenInclude(a => a.User)
                .Include(p => p.Asset)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (problem == null) return NotFound();

            return View(problem);
        }

        public IActionResult Create(string? title = null, string? description = null, int? categoryId = null)
        {
            ViewBag.Categories = _db.TicketCategories.ToList();
            ViewBag.Users = _db.Users.ToList();

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdString, out int userId))
            {
                ViewBag.UserAssets = _db.Assets.Where(a => a.UserId == userId).ToList();
            }
            else
            {
                ViewBag.UserAssets = new List<Asset>();
            }
            
            var model = new Problem
            {
                Title = title ?? "",
                Description = description ?? "",
                CategoryId = categoryId
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Problem problem)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            problem.CreatedById = userId;
            problem.CreatedAt = DateTime.Now;
            problem.Status = "Open";

            ModelState.Remove(nameof(problem.Asset));
            ModelState.Remove(nameof(problem.Creator));

            if (ModelState.IsValid)
            {
                _db.Add(problem);
                await _db.SaveChangesAsync();

                await LogActivity(problem.Id, "Created", "Problem record was created.");
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categories = _db.TicketCategories.ToList();
            ViewBag.Users = _db.Users.ToList();
            
            var uidStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(uidStr, out int uid))
            {
                ViewBag.UserAssets = _db.Assets.Where(a => a.UserId == uid).ToList();
            }
            else
            {
                ViewBag.UserAssets = new List<Asset>();
            }

            return View(problem);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var problem = await _db.Problems.FindAsync(id);
            if (problem == null) return NotFound();

            ViewBag.Categories = _db.TicketCategories.ToList();
            ViewBag.Users = _db.Users.ToList();

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdString, out int userId))
            {
                ViewBag.UserAssets = _db.Assets.Where(a => a.UserId == userId).ToList();
            }
            else
            {
                ViewBag.UserAssets = new List<Asset>();
            }

            return View(problem);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Problem problem)
        {
            if (id != problem.Id) return NotFound();

            ModelState.Remove(nameof(problem.Asset));
            ModelState.Remove(nameof(problem.Creator));

            if (ModelState.IsValid)
            {
                try
                {
                    var existing = await _db.Problems.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
                    problem.UpdatedAt = DateTime.Now;
                    
                    if (problem.Status == "Closed" && existing?.Status != "Closed")
                    {
                        problem.ClosedAt = DateTime.Now;
                    }

                    _db.Update(problem);
                    await _db.SaveChangesAsync();
                    
                    await LogActivity(problem.Id, "Updated", "Problem details were updated.");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProblemExists(problem.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Details), new { id = problem.Id });
            }

            ViewBag.Categories = _db.TicketCategories.ToList();
            ViewBag.Users = _db.Users.ToList();
            
            var uidStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(uidStr, out int uid))
            {
                ViewBag.UserAssets = _db.Assets.Where(a => a.UserId == uid).ToList();
            }
            else
            {
                ViewBag.UserAssets = new List<Asset>();
            }

            return View(problem);
        }

        [HttpPost]
        public async Task<IActionResult> LinkIncident(int problemId, int ticketId)
        {
            var exists = await _db.ProblemIncidents.AnyAsync(pi => pi.ProblemId == problemId && pi.TicketId == ticketId);
            if (!exists)
            {
                var link = new ProblemIncident
                {
                    ProblemId = problemId,
                    TicketId = ticketId,
                    LinkedAt = DateTime.Now
                };
                _db.ProblemIncidents.Add(link);
                await _db.SaveChangesAsync();

                await LogActivity(problemId, "Incident Linked", $"Incident #{ticketId} was linked to this problem.");
            }
            return RedirectToAction(nameof(Details), new { id = problemId });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateRCA(int id, string rootCause, string workaround, string permanentFix, string rcaMethod, string investigationNotes, string status)
        {
            var problem = await _db.Problems.FindAsync(id);
            if (problem == null) return NotFound();

            problem.RootCause = rootCause;
            problem.Workaround = workaround;
            problem.PermanentFix = permanentFix;
            problem.RCAMethod = rcaMethod;
            problem.InvestigationNotes = investigationNotes;
            problem.Status = status;
            problem.UpdatedAt = DateTime.Now;

            if (status == "Closed") problem.ClosedAt = DateTime.Now;

            _db.Update(problem);
            await _db.SaveChangesAsync();

            await LogActivity(id, "RCA Updated", "Root Cause Analysis and status were updated.");

            return RedirectToAction(nameof(Details), new { id = id });
        }

        [HttpGet]
        public async Task<IActionResult> GetLinkableIncidents(string term)
        {
            var tickets = await _db.Tickets
                .Where(t => t.Subject!.Contains(term) || t.Id.ToString() == term)
                .Select(t => new { id = t.Id, text = $"#{t.Id} - {t.Subject}" })
                .Take(10)
                .ToListAsync();

            return Json(tickets);
        }

        private bool ProblemExists(int id) => _db.Problems.Any(e => e.Id == id);

        private async Task LogActivity(int problemId, string action, string details)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var activity = new ProblemActivity
            {
                ProblemId = problemId,
                Action = action,
                Details = details,
                UserId = userId,
                CreatedAt = DateTime.Now
            };
            _db.ProblemActivities.Add(activity);
            await _db.SaveChangesAsync();
        }
    }
}
