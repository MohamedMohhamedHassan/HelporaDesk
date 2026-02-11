using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using ServiceCore.Data;
using ServiceCore.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace ServiceCore.Controllers
{
    public class SolutionsController : Controller
    {
        private readonly ServiceCoreDbContext _db;

        public SolutionsController(ServiceCoreDbContext db)
        {
            _db = db;
        }

        // GET: Solutions
        public IActionResult Index(string search = "", int? topicId = null, string status = "Published")
        {
            var query = _db.Solutions
                .Include(s => s.Topic)
                .Include(s => s.Creator)
                .Include(s => s.Owner)
                .AsQueryable();

            // Admins can see all statuses, regular users only Published
            if (!User.IsInRole("Admin"))
            {
                query = query.Where(s => s.Status == "Published");
                ViewBag.SelectedStatus = "Published";
            }
            else
            {
                if (!string.IsNullOrEmpty(status))
                    query = query.Where(s => s.Status == status);
                ViewBag.SelectedStatus = status;
            }

            // Filter by topic
            if (topicId.HasValue)
                query = query.Where(s => s.TopicId == topicId.Value);

            // Search by title, content, or keywords
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(s =>
                    (s.Title != null && s.Title.Contains(search)) ||
                    (s.Content != null && s.Content.Contains(search)) ||
                    (s.Keywords != null && s.Keywords.Contains(search)));
            }

            var solutions = query
                .OrderByDescending(s => s.CreatedAt)
                .ToList();

            ViewBag.Topics = _db.SolutionTopics.ToList();
            ViewBag.SelectedTopicId = topicId;
            ViewBag.Search = search;

            return View(solutions);
        }

        // GET: Solutions/Dashboard
        [Authorize(Roles = "Admin")]
        public IActionResult Dashboard()
        {
            var totalCount = _db.Solutions.Count();
            var publishedCount = _db.Solutions.Count(s => s.Status == "Published");
            var draftCount = _db.Solutions.Count(s => s.Status == "Draft");
            var pendingCount = _db.Solutions.Count(s => s.Status == "Approved");
            var expiredCount = _db.Solutions.Count(s => s.Status == "Expired");

            var mostViewed = _db.Solutions
                .Include(s => s.Topic)
                .OrderByDescending(s => s.Views)
                .Take(5)
                .ToList();

            var contributors = _db.Solutions
                .Include(s => s.Creator)
                .GroupBy(s => s.Creator!.Name)
                .Select(g => new { Name = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .Take(5)
                .Cast<dynamic>()
                .ToList();

            var needsReview = _db.Solutions
                .Where(s => s.ReviewDate.HasValue && s.ReviewDate.Value <= DateTime.Now && s.Status != "Expired")
                .Include(s => s.Topic)
                .OrderBy(s => s.ReviewDate)
                .ToList();

            ViewBag.TotalCount = totalCount;
            ViewBag.PublishedCount = publishedCount;
            ViewBag.DraftCount = draftCount;
            ViewBag.PendingCount = pendingCount;
            ViewBag.ExpiredCount = expiredCount;
            ViewBag.MostViewed = mostViewed;
            ViewBag.Contributors = contributors;
            ViewBag.NeedsReview = needsReview;

            return View();
        }

        // GET: Solutions/Details/5
        public IActionResult Details(int id)
        {
            var solution = _db.Solutions
                .Include(s => s.Topic)
                .Include(s => s.Creator)
                .Include(s => s.Owner)
                .Include(s => s.Attachments)
                    .ThenInclude(a => a.User)
                .FirstOrDefault(s => s.Id == id);

            if (solution == null)
                return NotFound();

            // Increment view count
            solution.Views++;
            _db.SaveChanges();

            return View(solution);
        }

        // GET: Solutions/Create
        [Authorize]
        public IActionResult Create()
        {
            ViewBag.Topics = _db.SolutionTopics.ToList();
            ViewBag.Users = _db.Users.ToList();
            return View();
        }

        // POST: Solutions/Create
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create(Solution solution, List<IFormFile>? attachments, string? status)
        {
            ModelState.Remove(nameof(solution.Topic));
            ModelState.Remove(nameof(solution.Creator));
            ModelState.Remove(nameof(solution.Owner));

            if (!ModelState.IsValid)
            {
                ViewBag.Topics = _db.SolutionTopics.ToList();
                ViewBag.Users = _db.Users.ToList();
                return View(solution);
            }

            var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId))
            {
                return Unauthorized();
            }

            solution.CreatedBy = userId;
            solution.CreatedAt = DateTime.Now;
            
            if (!string.IsNullOrEmpty(status))
            {
                solution.Status = status;
            }

            _db.Solutions.Add(solution);
            await _db.SaveChangesAsync();

            // Handle file uploads
            if (attachments != null && attachments.Any())
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "solutions");
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

                        var attachment = new SolutionAttachment
                        {
                            SolutionId = solution.Id,
                            FileName = fileName,
                            FilePath = $"/uploads/solutions/{uniqueFileName}",
                            UploadedAt = DateTime.Now,
                            UserId = userId
                        };

                        _db.SolutionAttachments.Add(attachment);
                    }
                }

                await _db.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Solutions/Edit/5
        [Authorize]
        public IActionResult Edit(int id)
        {
            var solution = _db.Solutions.FirstOrDefault(s => s.Id == id);
            if (solution == null)
                return NotFound();

            ViewBag.Topics = _db.SolutionTopics.ToList();
            ViewBag.Users = _db.Users.ToList();
            return View(solution);
        }

        // POST: Solutions/Edit/5
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Edit(Solution solution)
        {
            ModelState.Remove(nameof(solution.Topic));
            ModelState.Remove(nameof(solution.Creator));
            ModelState.Remove(nameof(solution.Owner));

            if (!ModelState.IsValid)
            {
                ViewBag.Topics = _db.SolutionTopics.ToList();
                ViewBag.Users = _db.Users.ToList();
                return View(solution);
            }

            var existing = _db.Solutions.Find(solution.Id);
            if (existing == null)
                return NotFound();

            existing.Title = solution.Title;
            existing.Content = solution.Content;
            existing.TopicId = solution.TopicId;
            existing.Keywords = solution.Keywords;
            existing.OwnerId = solution.OwnerId;
            existing.ReviewDate = solution.ReviewDate;
            existing.ExpiryDate = solution.ExpiryDate;

            _db.Solutions.Update(existing);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = solution.Id });
        }

        // POST: Solutions/Approve/5
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult Approve(int id)
        {
            var solution = _db.Solutions.Find(id);
            if (solution == null)
                return NotFound();

            solution.Status = "Approved";
            _db.SaveChanges();

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Solutions/Publish/5
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult Publish(int id)
        {
            var solution = _db.Solutions.Find(id);
            if (solution == null)
                return NotFound();

            solution.Status = "Published";
            _db.SaveChanges();

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Solutions/Delete/5
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult Delete(int id)
        {
            var solution = _db.Solutions.Find(id);
            if (solution == null)
                return NotFound();

            _db.Solutions.Remove(solution);
            _db.SaveChanges();

            return RedirectToAction(nameof(Index));
        }
    }
}
