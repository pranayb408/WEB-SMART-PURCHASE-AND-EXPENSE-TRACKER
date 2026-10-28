using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalExpenseTracker.Data;
using PersonalExpenseTracker.Models;

namespace PersonalExpenseTracker.Controllers
{
    public class PlatformsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PlatformsController> _logger;

        public PlatformsController(AppDbContext context, ILogger<PlatformsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: /Platforms
        public async Task<IActionResult> Index()
        {
            var platforms = await _context.Platforms
                .AsNoTracking()
                .Include(p => p.PurchaseOrders)
                .OrderBy(p => p.PlatformType)
                .ThenBy(p => p.Name)
                .ToListAsync();

            return View(platforms);
        }

        // POST: /Platforms/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Platform platform)
        {
            if (string.IsNullOrWhiteSpace(platform.Name))
            {
                ModelState.AddModelError("Name", "Platform name is required.");
            }
            else
            {
                string cleanName = platform.Name.Trim();
                bool exists = await _context.Platforms.AnyAsync(p => p.Name == cleanName);
                if (exists)
                {
                    ModelState.AddModelError("Name", $"A platform named '{cleanName}' already exists.");
                }
                else
                {
                    platform.Name = cleanName;
                    platform.PlatformType = string.IsNullOrWhiteSpace(platform.PlatformType) ? "Other" : platform.PlatformType.Trim();
                    platform.IsActive = true;

                    _context.Platforms.Add(platform);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Platform '{platform.Name}' added successfully.";
                    return RedirectToAction(nameof(Index));
                }
            }

            var platforms = await _context.Platforms
                .AsNoTracking()
                .Include(p => p.PurchaseOrders)
                .OrderBy(p => p.PlatformType)
                .ThenBy(p => p.Name)
                .ToListAsync();

            ViewBag.CreateError = true;
            return View("Index", platforms);
        }

        // POST: /Platforms/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string name, string platformType)
        {
            var platform = await _context.Platforms.FindAsync(id);
            if (platform == null) return NotFound();

            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["ErrorMessage"] = "Platform name cannot be empty.";
                return RedirectToAction(nameof(Index));
            }

            string cleanName = name.Trim();
            bool duplicate = await _context.Platforms.AnyAsync(p => p.Id != id && p.Name == cleanName);
            if (duplicate)
            {
                TempData["ErrorMessage"] = $"A platform named '{cleanName}' already exists.";
                return RedirectToAction(nameof(Index));
            }

            platform.Name = cleanName;
            if (!string.IsNullOrWhiteSpace(platformType))
            {
                platform.PlatformType = platformType.Trim();
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Platform '{platform.Name}' updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Platforms/ToggleActive/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var platform = await _context.Platforms.FindAsync(id);
            if (platform == null) return NotFound();

            platform.IsActive = !platform.IsActive;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Platform '{platform.Name}' status changed to {(platform.IsActive ? "Active" : "Inactive")}.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Platforms/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var platform = await _context.Platforms
                .Include(p => p.PurchaseOrders)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (platform == null) return NotFound();

            // Historical reference protection: Check if platform is used in orders
            if (platform.PurchaseOrders != null && platform.PurchaseOrders.Any())
            {
                TempData["ErrorMessage"] = $"Cannot delete '{platform.Name}' because it is referenced by {platform.PurchaseOrders.Count} historical purchase order(s). You can deactivate it instead to hide it from new orders.";
                return RedirectToAction(nameof(Index));
            }

            _context.Platforms.Remove(platform);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Platform '{platform.Name}' deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}

