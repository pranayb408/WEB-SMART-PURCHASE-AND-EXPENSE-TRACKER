using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalExpenseTracker.Data;
using PersonalExpenseTracker.Models;

namespace PersonalExpenseTracker.Controllers
{
    public class CategoriesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<CategoriesController> _logger;

        public CategoriesController(AppDbContext context, ILogger<CategoriesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: /Categories
        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories
                .AsNoTracking()
                .Include(c => c.OrderItems)
                .Include(c => c.CategoryBudgets)
                .OrderBy(c => c.Name)
                .ToListAsync();

            return View(categories);
        }

        // POST: /Categories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category)
        {
            if (string.IsNullOrWhiteSpace(category.Name))
            {
                ModelState.AddModelError("Name", "Category name is required.");
            }
            else
            {
                string cleanName = category.Name.Trim();
                bool exists = await _context.Categories.AnyAsync(c => c.Name == cleanName);
                if (exists)
                {
                    ModelState.AddModelError("Name", $"A category named '{cleanName}' already exists.");
                }
                else
                {
                    category.Name = cleanName;
                    category.CategoryType = string.IsNullOrWhiteSpace(category.CategoryType) ? "Expense" : category.CategoryType.Trim();
                    category.IsActive = true;

                    _context.Categories.Add(category);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Category '{category.Name}' added successfully.";
                    return RedirectToAction(nameof(Index));
                }
            }

            var categories = await _context.Categories
                .AsNoTracking()
                .Include(c => c.OrderItems)
                .Include(c => c.CategoryBudgets)
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewBag.CreateError = true;
            return View("Index", categories);
        }

        // POST: /Categories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string name, string categoryType)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();

            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["ErrorMessage"] = "Category name cannot be empty.";
                return RedirectToAction(nameof(Index));
            }

            string cleanName = name.Trim();
            bool duplicate = await _context.Categories.AnyAsync(c => c.Id != id && c.Name == cleanName);
            if (duplicate)
            {
                TempData["ErrorMessage"] = $"A category named '{cleanName}' already exists.";
                return RedirectToAction(nameof(Index));
            }

            category.Name = cleanName;
            if (!string.IsNullOrWhiteSpace(categoryType))
            {
                category.CategoryType = categoryType.Trim();
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Category '{category.Name}' updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Categories/ToggleActive/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();

            category.IsActive = !category.IsActive;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Category '{category.Name}' status changed to {(category.IsActive ? "Active" : "Inactive")}.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Categories/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.Categories
                .Include(c => c.OrderItems)
                .Include(c => c.CategoryBudgets)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null) return NotFound();

            int referenceCount = (category.OrderItems?.Count ?? 0) + (category.CategoryBudgets?.Count ?? 0);
            if (referenceCount > 0)
            {
                TempData["ErrorMessage"] = $"Cannot delete category '{category.Name}' because it is referenced by {category.OrderItems?.Count ?? 0} order item(s) and {category.CategoryBudgets?.Count ?? 0} budget(s). You can deactivate it instead to hide it from new entries without corrupting historical reports.";
                return RedirectToAction(nameof(Index));
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Category '{category.Name}' deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}

