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

            return View(ticket);
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var ticket = _db.Tickets.FirstOrDefault(t => t.Id == id);
            if (ticket == null) return NotFound();

            ViewBag.Statuses = _db.TicketStatuses.ToList();
            ViewBag.Priorities = _db.TicketPriorities.ToList();
            ViewBag.Categories = _db.TicketCategories.ToList();
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
                ViewBag.Categories = _db.TicketCategories.ToList();
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
    }
}
