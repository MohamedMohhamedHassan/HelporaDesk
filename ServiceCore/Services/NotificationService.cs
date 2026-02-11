using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ServiceCore.Data;
using ServiceCore.Models;

namespace ServiceCore.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ServiceCoreDbContext _db;
        private readonly IEmailService _emailService;

        public NotificationService(ServiceCoreDbContext db, IEmailService emailService)
        {
            _db = db;
            _emailService = emailService;
        }

        public async Task NotifyUserAsync(int userId, string title, string message, string? linkAction = null)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null) return;

            // 1. Create In-App Notification
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                LinkAction = linkAction,
                CreatedAt = DateTime.Now,
                IsRead = false
            };

            _db.Notifications.Add(notification);
            await _db.SaveChangesAsync();

            // 2. Send Email
            if (!string.IsNullOrEmpty(user.Email))
            {
                await _emailService.SendEmailAsync(user.Email, title, message);
            }
        }

        public async Task NotifyTicketUpdateAsync(int ticketId, string message)
        {
            var ticket = await _db.Tickets
                .Include(t => t.Requester)
                .Include(t => t.Assigned)
                .FirstOrDefaultAsync(t => t.Id == ticketId);

            if (ticket == null) return;

            // Notify Requester
            if (ticket.RequesterId > 0)
            {
                await NotifyUserAsync(ticket.RequesterId, $"Ticket #{ticket.Id} Updated", message, $"/Tickets/Details/{ticket.Id}");
            }

            // Notify Assignee
            if (ticket.AssignedId.HasValue && ticket.AssignedId.Value != ticket.RequesterId)
            {
                await NotifyUserAsync(ticket.AssignedId.Value, $"Ticket #{ticket.Id} Updated", message, $"/TicketsManager/Details/{ticket.Id}");
            }
        }

        public async Task NotifyNewCommentAsync(int ticketId, int commentId)
        {
            var ticket = await _db.Tickets
                .Include(t => t.Requester)
                .Include(t => t.Assigned)
                .FirstOrDefaultAsync(t => t.Id == ticketId);

            var comment = await _db.TicketComments
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == commentId);

            if (ticket == null || comment == null) return;

            string message = $"{comment.User?.Name ?? "Someone"} added a comment: \"{(comment.Content.Length > 50 ? comment.Content.Substring(0, 47) + "..." : comment.Content)}\"";

            // If commenter is the requester, notify the assignee
            if (comment.UserId == ticket.RequesterId)
            {
                if (ticket.AssignedId.HasValue)
                {
                    await NotifyUserAsync(ticket.AssignedId.Value, $"New Comment on Ticket #{ticket.Id}", message, $"/TicketsManager/Details/{ticket.Id}");
                }
            }
            // If commenter is the assignee, notify the requester
            else if (ticket.AssignedId.HasValue && comment.UserId == ticket.AssignedId.Value)
            {
                await NotifyUserAsync(ticket.RequesterId, $"New Comment on Ticket #{ticket.Id}", message, $"/Tickets/Details/{ticket.Id}");
            }
            // Otherwise notify both if they are not the commenter
            else
            {
                if (comment.UserId != ticket.RequesterId)
                {
                    await NotifyUserAsync(ticket.RequesterId, $"New Comment on Ticket #{ticket.Id}", message, $"/Tickets/Details/{ticket.Id}");
                }
                if (ticket.AssignedId.HasValue && comment.UserId != ticket.AssignedId.Value)
                {
                    await NotifyUserAsync(ticket.AssignedId.Value, $"New Comment on Ticket #{ticket.Id}", message, $"/TicketsManager/Details/{ticket.Id}");
                }
            }
        }
    }
}
