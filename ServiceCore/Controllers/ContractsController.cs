using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceCore.Data;
using ServiceCore.Models;
using System.Security.Claims;

namespace ServiceCore.Controllers
{
    [Authorize]
    public class ContractsController : Controller
    {
        private readonly ServiceCoreDbContext _db;

        public ContractsController(ServiceCoreDbContext db)
        {
            _db = db;
        }

        // --- DASHBOARD ---
        public async Task<IActionResult> Index()
        {
            var activeContracts = await _db.Contracts
                .Include(c => c.Vendor)
                .Include(c => c.ContractType)
                .Where(c => c.Status == "Active")
                .ToListAsync();

            ViewBag.ExpiringSoon = await _db.Contracts
                .Where(c => c.Status == "Active" && c.EndDate <= DateTime.Now.AddDays(30))
                .CountAsync();

            ViewBag.TotalValue = activeContracts.Sum(c => c.Value);

            return View(activeContracts);
        }

        // --- CONTRACTS CRUD ---
        public async Task<IActionResult> Details(int id)
        {
            var contract = await _db.Contracts
                .Include(c => c.Vendor)
                .Include(c => c.ContractType)
                .Include(c => c.Attachments)
                .Include(c => c.Approvals)
                .Include(c => c.Payments)
                .Include(c => c.History)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (contract == null) return NotFound();

            return View(contract);
        }

        public async Task<IActionResult> Create()
        {
            await LoadDropdowns();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Contract contract)
        {
            if (ModelState.IsValid)
            {
                contract.CreatedAt = DateTime.UtcNow;
                contract.Status = "Draft";
                contract.CreatedById = GetCurrentUserId();

                _db.Add(contract);
                await _db.SaveChangesAsync();

                await LogHistory(contract.Id, "Created", "Contract created as Draft");
                return RedirectToAction(nameof(Details), new { id = contract.Id });
            }

            await LoadDropdowns();
            return View(contract);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var contract = await _db.Contracts.FindAsync(id);
            if (contract == null) return NotFound();

            await LoadDropdowns();
            return View(contract);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Contract contract)
        {
            if (ModelState.IsValid)
            {
                contract.UpdatedAt = DateTime.UtcNow;
                _db.Update(contract);
                await _db.SaveChangesAsync();

                await LogHistory(contract.Id, "Updated", "Contract details modified");
                return RedirectToAction(nameof(Details), new { id = contract.Id });
            }

            await LoadDropdowns();
            return View(contract);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitForApproval(int id)
        {
            var contract = await _db.Contracts.FindAsync(id);
            if (contract == null) return NotFound();

            if (contract.Status == "Draft")
            {
                contract.Status = "Pending Approval";
                await _db.SaveChangesAsync();

                await LogHistory(contract.Id, "Submitted", "Contract submitted for internal approval");
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id, string? comments)
        {
            var contract = await _db.Contracts.FindAsync(id);
            if (contract == null) return NotFound();

            contract.Status = "Active";
            contract.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            _db.ContractApprovals.Add(new ContractApproval
            {
                ContractId = id,
                ApproverId = GetCurrentUserId() ?? 0,
                Status = "Approved",
                Comments = comments,
                DecisionDate = DateTime.UtcNow
            });

            await LogHistory(id, "Approved", "Contract approved and set to Active status");
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string? comments)
        {
            var contract = await _db.Contracts.FindAsync(id);
            if (contract == null) return NotFound();

            contract.Status = "Draft";
            contract.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            _db.ContractApprovals.Add(new ContractApproval
            {
                ContractId = id,
                ApproverId = GetCurrentUserId() ?? 0,
                Status = "Rejected",
                Comments = comments,
                DecisionDate = DateTime.UtcNow
            });

            await LogHistory(id, "Rejected", $"Contract rejected. Reason: {comments}");
            return RedirectToAction(nameof(Details), new { id });
        }

        public async Task<IActionResult> Reports()
        {
            var allContracts = await _db.Contracts
                .Include(c => c.Vendor)
                .Include(c => c.ContractType)
                .ToListAsync();

            ViewBag.TotalActiveValue = allContracts.Where(c => c.Status == "Active").Sum(c => c.Value);
            ViewBag.PendingApprovals = allContracts.Count(c => c.Status == "Pending Approval");
            ViewBag.ExpiringSoon = allContracts.Count(c => c.Status == "Active" && c.EndDate <= DateTime.Now.AddDays(30));

            return View(allContracts);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAttachment(int id, IFormFile file)
        {
            if (file == null || file.Length == 0) return RedirectToAction(nameof(Details), new { id });

            var contract = await _db.Contracts.FindAsync(id);
            if (contract == null) return NotFound();

            // Simple local storage for demo
            var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "contracts");
            if (!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);

            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(uploads, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var attachment = new ContractAttachment
            {
                ContractId = id,
                FileName = file.FileName,
                FilePath = "/uploads/contracts/" + fileName,
                FileType = file.ContentType,
                FileSize = file.Length,
                UploadedById = GetCurrentUserId(),
                UploadedAt = DateTime.UtcNow
            };

            _db.ContractAttachments.Add(attachment);
            await _db.SaveChangesAsync();

            await LogHistory(id, "Attachment", $"Uploaded document: {file.FileName}");

            return RedirectToAction(nameof(Details), new { id });
        }

        // --- HELPERS ---
        private async Task LoadDropdowns()
        {
            ViewBag.Vendors = await _db.Vendors.OrderBy(v => v.Name).ToListAsync();
            ViewBag.ContractTypes = await _db.ContractTypes.OrderBy(t => t.Name).ToListAsync();
        }

        private int? GetCurrentUserId()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out int userId)) return userId;
            return null;
        }

        private async Task LogHistory(int contractId, string action, string notes)
        {
            _db.ContractHistories.Add(new ContractHistory
            {
                ContractId = contractId,
                Action = action,
                Notes = notes,
                ChangedById = GetCurrentUserId(),
                Timestamp = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }

        // --- VENDORS ---
        public async Task<IActionResult> Vendors()
        {
            var vendors = await _db.Vendors.OrderBy(v => v.Name).ToListAsync();
            return View(vendors);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpsertVendor(Vendor vendor)
        {
            if (!ModelState.IsValid) return RedirectToAction(nameof(Vendors));

            if (vendor.Id > 0)
            {
                var existing = await _db.Vendors.FindAsync(vendor.Id);
                if (existing != null)
                {
                    existing.Name = vendor.Name;
                    existing.ContactPerson = vendor.ContactPerson;
                    existing.Email = vendor.Email;
                    existing.Phone = vendor.Phone;
                    existing.Address = vendor.Address;
                    existing.Website = vendor.Website;
                    existing.Description = vendor.Description;
                    _db.Update(existing);
                }
            }
            else
            {
                vendor.CreatedAt = DateTime.UtcNow;
                _db.Add(vendor);
            }
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Vendors));
        }

        // --- CONTRACT TYPES ---
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ContractTypes()
        {
            var types = await _db.ContractTypes.OrderBy(t => t.Name).ToListAsync();
            return View(types);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpsertContractType(ContractType type)
        {
            if (!ModelState.IsValid) return RedirectToAction(nameof(ContractTypes));

            if (type.Id > 0)
            {
                var existing = await _db.ContractTypes.FindAsync(type.Id);
                if (existing != null)
                {
                    existing.Name = type.Name;
                    existing.Description = type.Description;
                    _db.Update(existing);
                }
            }
            else
            {
                _db.Add(type);
            }
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(ContractTypes));
        }
    }
}
