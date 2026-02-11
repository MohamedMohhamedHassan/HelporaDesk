using System.Linq;
using Microsoft.AspNetCore.Mvc;
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
            var articles = _db.Articles.ToList();
            if (!articles.Any())
            {
                articles = new[] {
                    new Article { Id = 1, Title = "How to reset your password" },
                    new Article { Id = 2, Title = "Managing project members" }
                }.ToList();
            }

            ViewData["Articles"] = articles;
            return View();
        }
    }
}
