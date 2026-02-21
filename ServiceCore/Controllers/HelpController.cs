using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceCore.Models;
using ServiceCore.Data;

namespace ServiceCore.Controllers
{
    public class HelpController : Controller
    {
        private readonly ServiceCoreDbContext _db;

        public HelpController(ServiceCoreDbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Chat(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return Json(new { success = false, message = "Please ask a question." });

            var q = query.ToLower();
            var keywords = q.Split(new[] { ' ', '?', '!', '.', ',' }, StringSplitOptions.RemoveEmptyEntries);

            // 1. Database Metric Queries (Real-time assistant)
            if (q.Contains("how many") || q.Contains("count") || q.Contains("total"))
            {
                if (q.Contains("ticket"))
                {
                    var count = await _db.Tickets.CountAsync();
                    return Json(new { success = true, botResponse = $"There are currently {count} tickets in the system." });
                }
                if (q.Contains("project"))
                {
                    var count = await _db.Projects.CountAsync();
                    return Json(new { success = true, botResponse = $"I found {count} active projects in ServiceCore." });
                }
                if (q.Contains("user"))
                {
                    var count = await _db.Users.CountAsync();
                    return Json(new { success = true, botResponse = $"There are {count} registered users." });
                }
            }

            // 2. System Module Knowledge (The "Brain")
            var moduleKnowledge = new Dictionary<string, string>
            {
                { "ticket", "The Tickets module allows you to track issues and requests. You can create a ticket from the sidebar or by clicking 'New Ticket' in the top bar." },
                { "project", "Projects help you organize long-term work. You can manage tasks, milestones, and team members within each project." },
                { "asset", "The Assets module tracks equipment like laptops, monitors, and licenses. You can assign them to users and track maintenance history." },
                { "contract", "Contract management tracks your agreements with vendors, including value, expiry dates, and payment schedules." },
                { "solution", "Solutions are your internal knowledge base. You can write articles to help others fix common issues faster." },
                { "permission", "Permissions are managed in the Admin section. Only administrators can change what each role is allowed to do." },
                { "kanban", "The Kanban board provides a visual way to track ticket progress across different statuses." }
            };

            foreach (var kw in keywords)
            {
                if (moduleKnowledge.ContainsKey(kw))
                {
                    return Json(new { success = true, botResponse = moduleKnowledge[kw] });
                }
            }

            // 3. Fallback to KB Search (Knowledge Base)
            var solutions = _db.Solutions
                .Where(s => s.Title.ToLower().Contains(q) || s.Content.ToLower().Contains(q) || (s.Keywords != null && s.Keywords.ToLower().Contains(q)))
                .Select(s => new {
                    id = s.Id,
                    title = s.Title,
                    preview = s.Content.Length > 100 ? s.Content.Substring(0, 100) + "..." : s.Content,
                    type = "Solution"
                })
                .Take(3)
                .ToList();

            if (solutions.Any())
            {
                var response = "I found some relevant solutions for you:";
                return Json(new { success = true, botResponse = response, results = solutions });
            }

            // 4. Ultimate Fallback
            return Json(new { success = true, botResponse = "I'm not exactly sure about that, but I'm learning! Try asking about 'Tickets', 'Projects', or 'How many tickets' to see what I can do." });
        }
    }
}
