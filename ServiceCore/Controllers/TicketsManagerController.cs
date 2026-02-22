using System.Linq;
using Microsoft.AspNetCore.Mvc;
using ServiceCore.Data;
using ServiceCore.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace ServiceCore.Controllers
{
    [Authorize(Roles = "Admin,Agent,Technical")]
    public class TicketsManagerController : Controller
    {
        private readonly ServiceCoreDbContext _db;
        private readonly ServiceCore.Services.INotificationService _notificationService;

        public TicketsManagerController(ServiceCoreDbContext db, ServiceCore.Services.INotificationService notificationService)
        {
            _db = db;
            _notificationService = notificationService;
        }

        public IActionResult Index(string search, int? statusId, int? priorityId, string filter = "", int page = 1, int pageSize = 20)
        {
            var query = _db.Tickets
                .Include(t => t.Status)
                .Include(t => t.Priority)
                .Include(t => t.Category)
                .Include(t => t.Requester)
                .Include(t => t.Assigned)
                .AsQueryable();

            if (filter == "my")
            {
                var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
                query = query.Where(t => t.RequesterId == userId || t.AssignedId == userId);
            }

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(t => (t.Subject ?? "").Contains(search) || (t.Requester != null && t.Requester.Name!.Contains(search)));

            if (statusId.HasValue)
                query = query.Where(t => t.StatusId == statusId);

            if (priorityId.HasValue)
                query = query.Where(t => t.PriorityId == priorityId);

            var total = query.Count();
            var tickets = query.OrderByDescending(t => t.UpdatedAt ?? t.CreatedAt)
                               .Skip((page - 1) * pageSize)
                               .Take(pageSize)
                               .ToList();

            ViewData["TotalCount"] = total;
            ViewData["Page"] = page;
            ViewData["PageSize"] = pageSize;
            ViewData["Search"] = search;
            ViewData["StatusId"] = statusId;
            ViewData["PriorityId"] = priorityId;

            PopulateDropdowns();
            return View(tickets);
        }

        [HttpGet]
        public IActionResult Create()
        {
            PopulateDropdowns();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Ticket ticket, List<IFormFile>? attachments)
        {
            // Remove navigation properties from validation
            ModelState.Remove(nameof(ticket.Status));
            ModelState.Remove(nameof(ticket.Priority));
            ModelState.Remove(nameof(ticket.Category));
            ModelState.Remove(nameof(ticket.Requester));
            ModelState.Remove(nameof(ticket.Assigned));

            if (!ModelState.IsValid)
            {
                PopulateDropdowns();
                return View(ticket);
            }

            ticket.CreatedAt = DateTime.Now;

            // Calculate SLA Due Date
            var priority = await _db.TicketPriorities.FindAsync(ticket.PriorityId);
            if (priority != null)
            {
                ticket.CalculateDueDate(priority.Name ?? "Medium");
            }

            _db.Tickets.Add(ticket);
            await _db.SaveChangesAsync();

            // Handle file uploads
            if (attachments != null && attachments.Any())
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "tickets");
                Directory.CreateDirectory(uploadsFolder);

                // Get current user ID (default to 1 for now, should use actual logged-in user)
                var userId = User.Identity?.IsAuthenticated == true
                    ? int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "1")
                    : 1;

                foreach (var file in attachments.Take(5)) // Max 5 files
                {
                    if (file.Length > 0 && file.Length <= 10 * 1024 * 1024) // Max 10MB
                    {
                        var fileName = Path.GetFileName(file.FileName);
                        var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        var attachment = new TicketAttachment
                        {
                            TicketId = ticket.Id,
                            FileName = fileName,
                            FilePath = $"/uploads/tickets/{uniqueFileName}",
                            UploadedAt = DateTime.Now,
                            UserId = userId
                        };

                        _db.TicketAttachments.Add(attachment);
                    }
                }

                await _db.SaveChangesAsync();
            }

            // Notify Requester
            await _notificationService.NotifyUserAsync(ticket.RequesterId, "Ticket Created", $"Your ticket #{ticket.Id} - {ticket.Subject} has been created successfully.", $"/Tickets/Details/{ticket.Id}");

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var ticket = _db.Tickets.FirstOrDefault(t => t.Id == id);
            if (ticket == null) return NotFound();

            PopulateDropdowns();
            return View(ticket);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Ticket ticket)
        {
            // Remove navigation properties from validation
            ModelState.Remove(nameof(ticket.Status));
            ModelState.Remove(nameof(ticket.Priority));
            ModelState.Remove(nameof(ticket.Category));
            ModelState.Remove(nameof(ticket.Requester));
            ModelState.Remove(nameof(ticket.Assigned));

            if (!ModelState.IsValid)
            {
                PopulateDropdowns();
                return View(ticket);
            }

            var existing = _db.Tickets.FirstOrDefault(t => t.Id == ticket.Id);
            if (existing == null) return NotFound();

            // Recalculate Due Date if priority changed
            if (existing.PriorityId != ticket.PriorityId)
            {
                var newPriority = _db.TicketPriorities.Find(ticket.PriorityId);
                if (newPriority != null)
                {
                    existing.CalculateDueDate(newPriority.Name ?? "Medium");
                }
            }

            // Set ResolutionDate if status changed to Closed
            var closedStatus = _db.TicketStatuses.FirstOrDefault(s => s.Name == "Closed");
            if (closedStatus != null && ticket.StatusId == closedStatus.Id && existing.StatusId != closedStatus.Id)
            {
                existing.ResolutionDate = DateTime.Now;
            }
            else if (closedStatus != null && ticket.StatusId != closedStatus.Id && existing.StatusId == closedStatus.Id)
            {
                existing.ResolutionDate = null; // Reopened
            }

            existing.Subject = ticket.Subject;
            existing.Description = ticket.Description;
            existing.StatusId = ticket.StatusId;
            existing.PriorityId = ticket.PriorityId;
            existing.CategoryId = ticket.CategoryId;
            existing.AssignedId = ticket.AssignedId;
            existing.UpdatedAt = DateTime.Now;

            _db.Tickets.Update(existing);
            _db.SaveChanges();

            await _notificationService.NotifyTicketUpdateAsync(existing.Id, $"Ticket #{existing.Id} was updated by a manager.");

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var ticket = _db.Tickets.FirstOrDefault(t => t.Id == id);
            if (ticket == null) return NotFound();
            _db.Tickets.Remove(ticket);
            _db.SaveChanges();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Details(int id)
        {
            var ticket = _db.Tickets
                .Include(t => t.Status)
                .Include(t => t.Priority)
                .Include(t => t.Category)
                .Include(t => t.Requester)
                .Include(t => t.Assigned)
                .Include(t => t.Comments)
                    .ThenInclude(c => c.User)
                .Include(t => t.Attachments)
                    .ThenInclude(a => a.User)
                .FirstOrDefault(t => t.Id == id);

            if (ticket == null) return NotFound();

            // Populate dropdowns for reassignment modal
            PopulateDropdowns();
            return View(ticket);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Resolve(int id)
        {
            var ticket = await _db.Tickets.FindAsync(id);
            if (ticket == null) return NotFound();

            var resolvedStatus = await _db.TicketStatuses.FirstOrDefaultAsync(s => s.Name == "Resolved");
            var closedStatus = await _db.TicketStatuses.FirstOrDefaultAsync(s => s.Name == "Closed");

            if (resolvedStatus != null)
            {
                ticket.StatusId = resolvedStatus.Id;
            }
            else if (closedStatus != null)
            {
                ticket.StatusId = closedStatus.Id;
            }

            ticket.ResolutionDate = DateTime.Now;
            ticket.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();

            await _notificationService.NotifyTicketUpdateAsync(ticket.Id, $"Ticket #{ticket.Id} marked as resolved.");

            TempData["Success"] = "Ticket resolved.";
            return RedirectToAction(nameof(Details), new { id = ticket.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reassign(int ticketId, int assignedId)
        {
            var ticket = await _db.Tickets.FindAsync(ticketId);
            if (ticket == null) return NotFound();

            ticket.AssignedId = assignedId == 0 ? null : assignedId;
            ticket.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();

            if (ticket.AssignedId.HasValue)
            {
                await _notificationService.NotifyUserAsync(ticket.AssignedId.Value, "Ticket Assigned", $"You have been assigned ticket #{ticket.Id}: {ticket.Subject}", $"/Tickets/Details/{ticket.Id}");
            }

            TempData["Success"] = "Ticket reassigned.";
            return RedirectToAction(nameof(Details), new { id = ticket.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendReminder(int id)
        {
            var ticket = await _db.Tickets.Include(t => t.Assigned).FirstOrDefaultAsync(t => t.Id == id);
            if (ticket == null) return NotFound();

            if (!ticket.AssignedId.HasValue)
            {
                TempData["Error"] = "Ticket is unassigned.";
                return RedirectToAction(nameof(Details), new { id = ticket.Id });
            }

            await _notificationService.NotifyUserAsync(ticket.AssignedId.Value, "Ticket Reminder", $"Reminder: ticket #{ticket.Id} - {ticket.Subject}", $"/Tickets/Details/{ticket.Id}");

            TempData["Success"] = "Reminder sent to assigned user.";
            return RedirectToAction(nameof(Details), new { id = ticket.Id });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int ticketId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return RedirectToAction(nameof(Details), new { id = ticketId });

            var ticket = _db.Tickets.Find(ticketId);
            if (ticket == null) return NotFound();

            var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId))
            {
                return Unauthorized();
            }

            var comment = new TicketComment
            {
                TicketId = ticketId,
                Content = content,
                CreatedAt = DateTime.Now,
                UserId = userId
            };

            _db.TicketComments.Add(comment);

            // Update ticket timestamp
            ticket.UpdatedAt = DateTime.Now;
            _db.Tickets.Update(ticket);

            _db.SaveChanges();

            await _notificationService.NotifyNewCommentAsync(ticket.Id, comment.Id);

            return RedirectToAction(nameof(Details), new { id = ticketId });
        }

        private void PopulateDropdowns()
        {
            ViewBag.Statuses = _db.TicketStatuses.ToList();
            ViewBag.Priorities = _db.TicketPriorities.ToList();

            // Get Hierarchical Categories for dropdown
            var allCategories = _db.TicketCategories.Include(c => c.Children).ToList();
            var flatCategories = new List<dynamic>();
            foreach (var root in allCategories.Where(c => c.ParentId == null).OrderBy(c => c.Name))
            {
                FlattenCategories(root, flatCategories, 0);
            }
            ViewBag.Categories = flatCategories;

            ViewBag.Users = _db.Users.ToList(); // For assignment
        }

        private void FlattenCategories(TicketCategory category, List<dynamic> list, int level)
        {
            list.Add(new { Id = category.Id, Name = new string('\u00A0', level * 4) + (level > 0 ? "└─ " : "") + category.Name });
            if (category.Children != null)
            {
                foreach (var child in category.Children.OrderBy(c => c.Name))
                {
                    FlattenCategories(child, list, level + 1);
                }
            }
        }
    }
}
