using System.Linq;
using Microsoft.AspNetCore.Mvc;
using ServiceCore.Data;
using ServiceCore.Models;

namespace ServiceCore.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SettingsApiController : ControllerBase
    {
        private readonly ServiceCoreDbContext _db;

        public SettingsApiController(ServiceCoreDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public IActionResult GetAll() => Ok(_db.Settings.ToList());

        [HttpPost]
        public IActionResult Save([FromBody] Setting s)
        {
            var existing = _db.Settings.FirstOrDefault(x => x.Key == s.Key);
            if (existing == null)
            {
                _db.Settings.Add(s);
            }
            else
            {
                existing.Value = s.Value;
                _db.Settings.Update(existing);
            }
            _db.SaveChanges();
            return Ok();
        }
    }
}
