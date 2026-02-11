using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceCore.Data;
using System.Security.Claims;

namespace ServiceCore.Controllers
{
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly ServiceCoreDbContext _db;

        public NotificationsController(ServiceCoreDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetUnread()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (userId == 0) return Json(new { count = 0, items = new object[] { } });

            var notifications = await _db.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .Take(5)
                .ToListAsync();

            return Json(new { 
                count = await _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead),
                items = notifications.Select(n => new {
                    n.Id,
                    n.Title,
                    n.Message,
                    n.CreatedAt,
                    n.LinkAction
                })
            });
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var notification = await _db.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
            
            if (notification != null)
            {
                notification.IsRead = true;
                await _db.SaveChangesAsync();
            }

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var unread = await _db.Notifications.Where(n => n.UserId == userId && !n.IsRead).ToListAsync();
            
            foreach (var n in unread)
            {
                n.IsRead = true;
            }

            await _db.SaveChangesAsync();
            return Ok();
        }
    }
}
