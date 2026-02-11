using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceCore.Data;
using ServiceCore.Models;

namespace ServiceCore.Controllers
{
    [Authorize]
    public class SettingsController : Controller
    {
        private readonly ServiceCoreDbContext _context;

        public SettingsController(ServiceCoreDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.Settings.ToListAsync());
        }

        [HttpPost]
        public async Task<IActionResult> Update(string key, string value)
        {
            // Simplified settings update
             var setting = await _context.Settings.FirstOrDefaultAsync(s => s.Key == key);
             if (setting != null)
             {
                 setting.Value = value;
                 _context.Update(setting);
                 await _context.SaveChangesAsync();
             }
             return RedirectToAction(nameof(Index));
        }
    }
}
