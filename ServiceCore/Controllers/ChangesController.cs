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
    public class ChangesController : Controller
    {
        private readonly ServiceCoreDbContext _db;

        public ChangesController(ServiceCoreDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index(string search = "", string status = "", string type = "")
        {
            var query = _db.ChangeRequests
                .Include(c => c.RequestedBy)
                .Include(c => c.AssignedTo)
                .Include(c => c.Category)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(c => c.Title.Contains(search) || c.Description.Contains(search));
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(c => c.Status == status);
            }

            if (!string.IsNullOrEmpty(type))
            {
                query = query.Where(c => c.Type == type);
            }

            var changes = await query.OrderByDescending(c => c.CreatedAt).ToListAsync();
            
            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.Type = type;

            return View(changes);
        }

        public async Task<IActionResult> Details(int id)
        {
            var change = await _db.ChangeRequests
                .Include(c => c.Category)
                .Include(c => c.RequestedBy)
                .Include(c => c.AssignedTo)
                .Include(c => c.Approvals)
                    .ThenInclude(a => a.Approver)
                .Include(c => c.Tasks)
                    .ThenInclude(t => t.AssignedTo)
                .Include(c => c.Activities)
                    .ThenInclude(a => a.User)
                .Include(c => c.LinkedAssets)
                    .ThenInclude(la => la.Asset)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (change == null) return NotFound();

            ViewBag.Users = await _db.Users.Where(u => u.IsActive).ToListAsync();
            return View(change);
        }

        public IActionResult Create()
        {
            ViewBag.Categories = _db.TicketCategories.ToList();
            ViewBag.Users = _db.Users.Where(u => u.IsActive).ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ChangeRequest change)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            change.RequestedById = userId;
            change.CreatedAt = DateTime.Now;
            change.Status = "Draft";

            if (ModelState.IsValid)
            {
                _db.Add(change);
                await _db.SaveChangesAsync();

                await LogActivity(change.Id, "Created", "Change request was initiated as Draft.");
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categories = _db.TicketCategories.ToList();
            ViewBag.Users = _db.Users.Where(u => u.IsActive).ToList();
            return View(change);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var change = await _db.ChangeRequests.FindAsync(id);
            if (change == null) return NotFound();

            ViewBag.Categories = _db.TicketCategories.ToList();
            ViewBag.Users = _db.Users.Where(u => u.IsActive).ToListAsync();
            return View(change);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ChangeRequest change)
        {
            if (id != change.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    change.UpdatedAt = DateTime.Now;
                    _db.Update(change);
                    await _db.SaveChangesAsync();
                    await LogActivity(id, "Updated", "Change request details were updated.");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ChangeExists(change.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Details), new { id = change.Id });
            }
            return View(change);
        }

        [HttpPost]
        public async Task<IActionResult> Submit(int id)
        {
            var change = await _db.ChangeRequests.FindAsync(id);
            if (change == null) return NotFound();

            change.Status = "Submitted";
            change.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();

            await LogActivity(id, "Submitted", "Change request was submitted for review.");
            return RedirectToAction(nameof(Details), new { id = id });
        }

        [HttpPost]
        public async Task<IActionResult> AddApproval(int changeId, int approverId)
        {
            var approval = new ChangeApproval
            {
                ChangeRequestId = changeId,
                ApproverId = approverId,
                Status = "Pending"
            };
            _db.ChangeApprovals.Add(approval);
            await _db.SaveChangesAsync();

            await LogActivity(changeId, "Approval Requested", $"Approval requested from analyst ID {approverId}.");
            return RedirectToAction(nameof(Details), new { id = changeId });
        }

        [HttpPost]
        public async Task<IActionResult> ActionApproval(int approvalId, string status, string comments)
        {
            var approval = await _db.ChangeApprovals.FindAsync(approvalId);
            if (approval == null) return NotFound();

            approval.Status = status;
            approval.Comments = comments;
            approval.ActionedAt = DateTime.Now;
            await _db.SaveChangesAsync();

            await LogActivity(approval.ChangeRequestId, $"Approval {status}", $"Approval was {status.ToLower()} by the reviewer.");

            // If all approved, move status?
            var change = await _db.ChangeRequests.Include(c => c.Approvals).FirstAsync(c => c.Id == approval.ChangeRequestId);
            if (change.Approvals.All(a => a.Status == "Approved"))
            {
                change.Status = "Approved";
                await _db.SaveChangesAsync();
                await LogActivity(change.Id, "Status Changed", "All approvals received. Change moved to Approved status.");
            }
            else if (status == "Rejected")
            {
                change.Status = "Rejected";
                await _db.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Details), new { id = approval.ChangeRequestId });
        }

        [HttpPost]
        public async Task<IActionResult> AddTask(int changeId, string description, int? assignedToId, DateTime? dueDate)
        {
            var task = new ChangeTask
            {
                ChangeRequestId = changeId,
                Description = description,
                AssignedToId = assignedToId,
                DueDate = dueDate,
                Status = "Pending"
            };
            _db.ChangeTasks.Add(task);
            await _db.SaveChangesAsync();

            await LogActivity(changeId, "Task Added", $"Implementation task added: {description}");
            return RedirectToAction(nameof(Details), new { id = changeId });
        }

        [HttpPost]
        public async Task<IActionResult> CompleteTask(int taskId)
        {
            var task = await _db.ChangeTasks.FindAsync(taskId);
            if (task == null) return NotFound();

            task.Status = "Completed";
            task.CompletedAt = DateTime.Now;
            await _db.SaveChangesAsync();

            await LogActivity(task.ChangeRequestId, "Task Completed", $"Task completed: {task.Description}");
            return RedirectToAction(nameof(Details), new { id = task.ChangeRequestId });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var change = await _db.ChangeRequests.FindAsync(id);
            if (change == null) return NotFound();

            change.Status = status;
            change.UpdatedAt = DateTime.Now;
            
            if (status == "Closed") change.ClosedAt = DateTime.Now;
            if (status == "In Progress") change.ActualStartDate = DateTime.Now;
            if (status == "Implemented") change.ActualEndDate = DateTime.Now;

            await _db.SaveChangesAsync();
            await LogActivity(id, "Status Updated", $"Workflow status changed to {status}.");

            return RedirectToAction(nameof(Details), new { id = id });
        }

        private bool ChangeExists(int id) => _db.ChangeRequests.Any(e => e.Id == id);

        private async Task LogActivity(int changeId, string action, string details)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdString == null) return;
            var userId = int.Parse(userIdString);
            
            var activity = new ChangeActivity
            {
                ChangeRequestId = changeId,
                UserId = userId,
                Action = action,
                Details = details,
                CreatedAt = DateTime.Now
            };
            _db.ChangeActivities.Add(activity);
            await _db.SaveChangesAsync();
        }
    }
}
