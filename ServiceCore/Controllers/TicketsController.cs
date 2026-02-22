using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using ServiceCore.Models;
using ServiceCore.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Text;

namespace ServiceCore.Controllers
{
    public class TicketsController : Controller
    {
        private readonly ServiceCoreDbContext _db;
        private readonly ServiceCore.Services.INotificationService _notificationService;

        public TicketsController(ServiceCoreDbContext db, ServiceCore.Services.INotificationService notificationService)
        {
            _db = db;
            _notificationService = notificationService;
        }

        public IActionResult Index(string filter = "", string search = "", string timeFilter = "30", int page = 1, int pageSize = 20)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = int.TryParse(userIdString, out int id) ? id : 0;
            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

            var query = _db.Tickets
                .Include(t => t.Status)
                .Include(t => t.Priority)
                .Include(t => t.Category)
                .Include(t => t.Requester)
                .Include(t => t.Assigned)
                .AsQueryable();

            // Visibility rules
            if (role == "User" || role == "Member")
            {
                query = query.Where(t => t.RequesterId == userId);
            }
            else if (role == "Agent" || role == "Technical")
            {
                query = query.Where(t => t.RequesterId == userId || t.AssignedId == userId || t.AssignedId == null);
            }

            // Apply time filter
            if (!string.IsNullOrEmpty(timeFilter) && timeFilter.ToLower() != "all")
            {
                DateTime cutoff = DateTime.MinValue;
                switch (timeFilter.ToLower())
                {
                    case "30":
                        cutoff = DateTime.Now.AddDays(-30);
                        break;
                    case "7":
                        cutoff = DateTime.Now.AddDays(-7);
                        break;
                    case "today":
                    case "1":
                        cutoff = DateTime.Today;
                        break;
                }

                if (cutoff > DateTime.MinValue)
                {
                    query = query.Where(t => t.CreatedAt >= cutoff);
                }
            }

            // Apply search
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                query = query.Where(t => (t.Subject != null && t.Subject.Contains(s)) ||
                                         (t.Description != null && t.Description.Contains(s)) ||
                                         (t.Requester != null && t.Requester.Name != null && t.Requester.Name.Contains(s)) ||
                                         (t.Assigned != null && t.Assigned.Name != null && t.Assigned.Name.Contains(s)));
            }

            var total = query.Count();

            var tickets = query
                .OrderByDescending(t => t.UpdatedAt ?? t.CreatedAt)
                .Skip((Math.Max(1, page) - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewData["Tickets"] = tickets;
            ViewData["Filter"] = filter;
            ViewData["Search"] = search;
            ViewData["TimeFilter"] = timeFilter;
            ViewData["Page"] = Math.Max(1, page);
            ViewData["PageSize"] = pageSize;
            ViewData["TotalPages"] = (int)Math.Ceiling(total / (double)pageSize);
            ViewData["TotalCount"] = total;

            return View();
        }

        [HttpGet]
        public IActionResult ExportCsv(string filter = "", string search = "", string timeFilter = "")
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = int.TryParse(userIdString, out int id) ? id : 0;
            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

            var query = _db.Tickets
                .Include(t => t.Status)
                .Include(t => t.Priority)
                .Include(t => t.Category)
                .Include(t => t.Requester)
                .Include(t => t.Assigned)
                .AsQueryable();

            if (role == "User" || role == "Member")
            {
                query = query.Where(t => t.RequesterId == userId);
            }
            else if (role == "Agent" || role == "Technical")
            {
                query = query.Where(t => t.RequesterId == userId || t.AssignedId == userId || t.AssignedId == null);
            }

            if (!string.IsNullOrEmpty(timeFilter) && timeFilter.ToLower() != "all")
            {
                DateTime cutoff = DateTime.MinValue;
                switch (timeFilter.ToLower())
                {
                    case "30":
                        cutoff = DateTime.Now.AddDays(-30);
                        break;
                    case "7":
                        cutoff = DateTime.Now.AddDays(-7);
                        break;
                    case "today":
                    case "1":
                        cutoff = DateTime.Today;
                        break;
                }

                if (cutoff > DateTime.MinValue)
                {
                    query = query.Where(t => t.CreatedAt >= cutoff);
                }
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                query = query.Where(t => (t.Subject != null && t.Subject.Contains(s)) ||
                                         (t.Description != null && t.Description.Contains(s)) ||
                                         (t.Requester != null && t.Requester.Name != null && t.Requester.Name.Contains(s)) ||
                                         (t.Assigned != null && t.Assigned.Name != null && t.Assigned.Name.Contains(s)));
            }

            var list = query.OrderByDescending(t => t.UpdatedAt ?? t.CreatedAt).ToList();

            var sb = new StringBuilder();
            sb.AppendLine("Id,Subject,Status,Priority,Requester,Assigned,CreatedAt,UpdatedAt");
            foreach (var t in list)
            {
                var subject = (t.Subject ?? "").Replace("\"", "\"\"");
                var status = t.Status?.Name ?? "";
                var priority = t.Priority?.Name ?? "";
                var requester = t.Requester?.Name ?? "";
                var assigned = t.Assigned?.Name ?? "";
                var created = t.CreatedAt.ToString("o");
                var updated = (t.UpdatedAt ?? t.CreatedAt).ToString("o");
                sb.AppendLine($"{t.Id},\"{subject}\",{status},{priority},\"{requester}\",\"{assigned}\",{created},{updated}");
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv", $"tickets_{DateTime.Now:yyyyMMddHHmm}.csv");
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
                .FirstOrDefault(t => t.Id == id);

            if (ticket == null) return NotFound();

            // Get related solutions based on category and keywords
            var categoryName = ticket.Category?.Name?.ToLower() ?? "";
            var keywords = (ticket.Subject + " " + ticket.Description)?.ToLower() ?? "";

            var relatedSolutions = _db.Solutions
                .Include(s => s.Topic)
                .Where(s => s.Status == "Published")
                .Where(s =>
                    (s.Topic != null && categoryName.Contains(s.Topic.Name.ToLower())) ||
                    (s.Title != null && keywords.Contains(s.Title.ToLower())) ||
                    (s.Keywords != null && keywords.Contains(s.Keywords.ToLower())))
                .OrderByDescending(s => s.Views)
                .Take(5)
                .ToList();

            ViewBag.RelatedSolutions = relatedSolutions;

            return View(ticket);
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var ticket = _db.Tickets.FirstOrDefault(t => t.Id == id);
            if (ticket == null) return NotFound();

            ViewBag.Statuses = _db.TicketStatuses.ToList();
            ViewBag.Priorities = _db.TicketPriorities.ToList();

            // Hierarchical Categories
            var allCategories = _db.TicketCategories.Include(c => c.Children).ToList();
            var flatCategories = new List<dynamic>();
            foreach (var root in allCategories.Where(c => c.ParentId == null).OrderBy(c => c.Name))
            {
                FlattenCategories(root, flatCategories, 0);
            }
            ViewBag.Categories = flatCategories;

            ViewBag.Users = _db.Users.ToList();

            return View(ticket);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Ticket ticket)
        {
            ModelState.Remove(nameof(ticket.Status));
            ModelState.Remove(nameof(ticket.Priority));
            ModelState.Remove(nameof(ticket.Category));
            ModelState.Remove(nameof(ticket.Requester));
            ModelState.Remove(nameof(ticket.Assigned));

            if (!ModelState.IsValid)
            {
                ViewBag.Statuses = _db.TicketStatuses.ToList();
                ViewBag.Priorities = _db.TicketPriorities.ToList();

                // Hierarchical Categories
                var allCategories = _db.TicketCategories.Include(c => c.Children).ToList();
                var flatCategories = new List<dynamic>();
                foreach (var root in allCategories.Where(c => c.ParentId == null).OrderBy(c => c.Name))
                {
                    FlattenCategories(root, flatCategories, 0);
                }
                ViewBag.Categories = flatCategories;

                ViewBag.Users = _db.Users.ToList();
                return View(ticket);
            }

            var existing = _db.Tickets.Find(ticket.Id);
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

            await _notificationService.NotifyTicketUpdateAsync(existing.Id, $"Ticket #{existing.Id} was updated by a user.");

            return RedirectToAction(nameof(Details), new { id = ticket.Id });
        }

        [Authorize]
        [HttpPost]
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

        [HttpPost]
        public IActionResult Delete(int id)
        {
            var ticket = _db.Tickets.FirstOrDefault(t => t.Id == id);
            if (ticket == null) return NotFound();

            _db.Tickets.Remove(ticket);
            _db.SaveChanges();

            return RedirectToAction(nameof(Index));
        }
        [HttpGet]
        public IActionResult Create()
        {
            // Hierarchical Categories
            var allCategories = _db.TicketCategories.Include(c => c.Children).ToList();
            var flatCategories = new List<dynamic>();
            foreach (var root in allCategories.Where(c => c.ParentId == null).OrderBy(c => c.Name))
            {
                FlattenCategories(root, flatCategories, 0);
            }
            ViewBag.Categories = flatCategories;

            ViewBag.Priorities = _db.TicketPriorities.ToList();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Ticket ticket, List<IFormFile>? attachments)
        {
            // Remove navigation properties and admin-only fields from validation
            ModelState.Remove(nameof(ticket.Status));
            ModelState.Remove(nameof(ticket.Priority)); // We set this manually if needed, or user selects it
            ModelState.Remove(nameof(ticket.Category));
            ModelState.Remove(nameof(ticket.Requester));
            ModelState.Remove(nameof(ticket.Assigned));

            if (!ModelState.IsValid)
            {
                // Hierarchical Categories
                var allCategories = _db.TicketCategories.Include(c => c.Children).ToList();
                var flatCategories = new List<dynamic>();
                foreach (var root in allCategories.Where(c => c.ParentId == null).OrderBy(c => c.Name))
                {
                    FlattenCategories(root, flatCategories, 0);
                }
                ViewBag.Categories = flatCategories;

                ViewBag.Priorities = _db.TicketPriorities.ToList();
                return View(ticket);
            }

            var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId))
            {
                return Unauthorized();
            }

            ticket.RequesterId = userId;
            ticket.CreatedAt = DateTime.Now;

            // Default Status to "Open" or "New"
            var openStatus = _db.TicketStatuses.FirstOrDefault(s => s.Name == "Open" || s.Name == "New");
            ticket.StatusId = openStatus?.Id ?? 1; // Fallback to 1 if not found

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

            // Notify Admins? (Optional - for now just success)
            await _notificationService.NotifyTicketUpdateAsync(ticket.Id, $"New ticket created by user.");

            return RedirectToAction(nameof(Index));
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
