using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceCore.Data;
using ServiceCore.Models;
using System.Security.Claims;

namespace ServiceCore.Controllers
{
    [Authorize]
    public class ApprovalsController : Controller
    {
        private readonly ServiceCoreDbContext _db;

        public ApprovalsController(ServiceCoreDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var role = User.FindFirstValue(ClaimTypes.Role);
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

            var query = _db.Approvals
                .Include(a => a.Requester)
                .Include(a => a.Approver)
                .AsQueryable();

            // If not admin/agent, only show things assigned to them or requested by them
            if (role != "Admin" && role != "Agent")
            {
                query = query.Where(a => a.RequesterId == userId || a.ApproverId == userId);
            }

            var items = await query.OrderByDescending(a => a.CreatedAt).ToListAsync();
            return View(items);
        }

        [HttpPost]
        public async Task<IActionResult> Action(int id, string status, string comments)
        {
            var approval = await _db.Approvals.FindAsync(id);
            if (approval == null) return NotFound();

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            
            approval.Status = status;
            approval.Comments = comments;
            approval.ActedAt = DateTime.UtcNow;
            approval.ApproverId = userId;

            // Handle Specific Side Effects
            if (status == "Approved")
            {
                if (approval.RequestType == "Ticket_Resolution" && approval.RelatedId.HasValue)
                {
                    var ticket = await _db.Tickets.FindAsync(approval.RelatedId.Value);
                    if (ticket != null)
                    {
                        var closedStatus = await _db.TicketStatuses.FirstOrDefaultAsync(s => s.Name == "Closed");
                        if (closedStatus != null)
                        {
                            ticket.StatusId = closedStatus.Id;
                            ticket.ResolutionDate = DateTime.UtcNow;
                        }
                    }
                }
            }

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> RequestApproval(string type, int relatedId, string subject, string description)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

            var request = new Approval
            {
                RequestType = type,
                RelatedId = relatedId,
                Subject = subject,
                Description = description,
                RequesterId = userId,
                Status = "Pending"
            };

            _db.Approvals.Add(request);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}
