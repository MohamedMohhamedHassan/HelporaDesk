using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using ServiceCore.Models;
using ServiceCore.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

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

        public IActionResult Index(string filter = "")
        {
            // Redirect Admins to the Manager view
            if (User.IsInRole("Admin"))
            {
                return RedirectToAction("Index", "TicketsManager");
            }

            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

            var query = _db.Tickets
                .Include(t => t.Status)
                .Include(t => t.Priority)
                .Include(t => t.Category)
                .Include(t => t.Requester)
                .Include(t => t.Assigned)
                .Where(t => t.RequesterId == userId || t.AssignedId == userId) // Strict filter for non-admins
                .AsQueryable();

            if (filter == "my")
            {
                 // Filter is already applied by default, but we keep the block if additional logic is needed
                 ViewData["Title"] = "My Tickets";
            }

            var tickets = query
                .OrderByDescending(t => t.UpdatedAt ?? t.CreatedAt)
                .ToList();

            ViewData["Tickets"] = tickets;
            ViewData["Filter"] = filter;
            return View();
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
