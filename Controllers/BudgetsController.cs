using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PersonalExpenseTracker.Data;
using PersonalExpenseTracker.Models;
using PersonalExpenseTracker.Services;
using PersonalExpenseTracker.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PersonalExpenseTracker.Controllers
{
    public class BudgetsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IFinancialAccountingService _accounting;
        private readonly ILogger<BudgetsController> _logger;

        public BudgetsController(
            AppDbContext context, 
            IFinancialAccountingService accounting,
            ILogger<BudgetsController> logger)
        {
            _context = context;
            _accounting = accounting;
            _logger = logger;
        }

        // GET: /Budgets/Index?year=2026&month=10
        public async Task<IActionResult> Index(int? year, int? month)
        {
            int currentYear = (year.HasValue && year.Value >= 2000 && year.Value <= 2100) 
                ? year.Value 
                : DateTime.Today.Year;

            int currentMonth = (month.HasValue && month.Value >= 1 && month.Value <= 12) 
                ? month.Value 
                : DateTime.Today.Month;

            ViewBag.SelectedYear = currentYear;
            ViewBag.SelectedMonth = currentMonth;

            var model = new BudgetIndexViewModel
            {
                SelectedYear = currentYear,
                SelectedMonth = currentMonth
            };

            DateTime startOfYear = new DateTime(currentYear, 1, 1);
            DateTime startOfNextYear = startOfYear.AddYears(1);

            DateTime startOfMonth = new DateTime(currentYear, currentMonth, 1);
            DateTime startOfNextMonth = startOfMonth.AddMonths(1);

            try
            {
                // 1. Query all monthly budgets for selected year
                var monthlyBudgets = await _context.MonthlyBudgets
                    .AsNoTracking()
                    .Where(b => b.Year == currentYear)
                    .OrderBy(b => b.Month)
                    .ToListAsync();

                // Compute realized spending per month: standalone expenses + completed purchase spending
                var yearExpenses = await _context.Transactions
                    .AsNoTracking()
                    .Where(t => t.Date >= startOfYear && t.Date < startOfNextYear && t.Type == "Expense")
                    .ToListAsync();

                var yearOrders = await _context.PurchaseOrders
                    .AsNoTracking()
                    .Where(p => p.OrderDate >= startOfYear && p.OrderDate < startOfNextYear && p.OrderStatus == OrderStatus.Completed)
                    .Select(p => new { p.OrderDate.Year, p.OrderDate.Month, NetSpent = p.FinalBillAmount - p.RefundAmount })
                    .ToListAsync();

                var monthlySpendingMap = new Dictionary<int, decimal>();
                for (int m = 1; m <= 12; m++)
                {
                    decimal se = yearExpenses.Where(t => t.Date.Month == m).Sum(t => t.Amount);
                    decimal po = yearOrders.Where(o => o.Month == m).Sum(o => o.NetSpent);
                    monthlySpendingMap[m] = se + po;
                }

                model.MonthlyBudgets = monthlyBudgets.Select(b => new MonthlyBudgetProgressItem
                {
                    Id = b.Id,
                    Year = b.Year,
                    Month = b.Month,
                    BudgetAmount = b.Amount,
                    ActualSpending = monthlySpendingMap.TryGetValue(b.Month, out decimal s) ? s : 0m
                }).ToList();

                // 2. Query Category Budgets for the selected (Year, Month)
                var categoryBudgets = await _context.CategoryBudgets
                    .AsNoTracking()
                    .Include(cb => cb.Category)
                    .Where(cb => cb.Year == currentYear && cb.Month == currentMonth)
                    .OrderBy(cb => cb.Category != null ? cb.Category.Name : string.Empty)
                    .ToListAsync();

                // Calculate actual category spending for selected month using proportional attribution
                var monthExpenses = await _context.Transactions
                    .AsNoTracking()
                    .Where(t => t.Date >= startOfMonth && t.Date < startOfNextMonth && t.Type == "Expense")
                    .ToListAsync();

                var monthOrders = await _context.PurchaseOrders
                    .AsNoTracking()
                    .Include(p => p.Items)
                        .ThenInclude(i => i.Category)
                    .Where(p => p.OrderDate >= startOfMonth && p.OrderDate < startOfNextMonth && p.OrderStatus == OrderStatus.Completed)
                    .ToListAsync();

                var categoryWiseSpending = _accounting.CalculateCategoryWiseSpending(monthExpenses, monthOrders);

                model.CategoryBudgets = categoryBudgets.Select(cb =>
                {
                    string catName = cb.Category?.Name ?? "Other";
                    decimal actual = categoryWiseSpending.TryGetValue(catName, out decimal spent) ? spent : 0m;

                    return new CategoryBudgetProgressViewModel
                    {
                        Id = cb.Id,
                        Year = cb.Year,
                        Month = cb.Month,
                        CategoryId = cb.CategoryId,
                        CategoryName = catName,
                        BudgetAmount = cb.Amount,
                        ActualSpending = actual
                    };
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not query budgets from database.");
                ViewBag.DatabaseOffline = true;
            }

            return View(model);
        }

        #region Monthly Budget CRUD (Section 9: Retain existing functionality)

        // GET: /Budgets/Create
        public IActionResult Create()
        {
            var budget = new MonthlyBudget
            {
                Month = DateTime.Today.Month,
                Year = DateTime.Today.Year
            };
            return View(budget);
        }

        // POST: /Budgets/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Year,Month,Amount")] MonthlyBudget budget)
        {
            if (budget.Month < 1 || budget.Month > 12)
            {
                ModelState.AddModelError(nameof(budget.Month), "Month must be between 1 and 12.");
            }

            if (budget.Year < 2000 || budget.Year > 2100)
            {
                ModelState.AddModelError(nameof(budget.Year), "Year must be between 2000 and 2100.");
            }

            if (budget.Amount <= 0)
            {
                ModelState.AddModelError(nameof(budget.Amount), "Budget amount must be greater than zero.");
            }

            try
            {
                bool exists = await _context.MonthlyBudgets
                    .AnyAsync(b => b.Year == budget.Year && b.Month == budget.Month);

                if (exists)
                {
                    ModelState.AddModelError(nameof(budget.Month), $"A monthly budget for {budget.Month}/{budget.Year} already exists.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not check duplicate budget in database.");
                ModelState.AddModelError("", "Database connection error.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(budget);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Budget of ₹{budget.Amount:N2} for {budget.Month}/{budget.Year} was set successfully.";
                    return RedirectToAction(nameof(Index), new { year = budget.Year, month = budget.Month });
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError("", "A budget for this month and year has already been recorded.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating monthly budget.");
                    ModelState.AddModelError("", "Unable to save budget to database.");
                }
            }

            return View(budget);
        }

        // GET: /Budgets/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || id <= 0) return NotFound();

            var budget = await _context.MonthlyBudgets.FindAsync(id);
            if (budget == null)
            {
                TempData["ErrorMessage"] = "Monthly budget not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(budget);
        }

        // POST: /Budgets/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Year,Month,Amount")] MonthlyBudget budget)
        {
            if (id != budget.Id) return BadRequest();

            if (budget.Month < 1 || budget.Month > 12)
            {
                ModelState.AddModelError(nameof(budget.Month), "Month must be between 1 and 12.");
            }

            if (budget.Year < 2000 || budget.Year > 2100)
            {
                ModelState.AddModelError(nameof(budget.Year), "Year must be between 2000 and 2100.");
            }

            if (budget.Amount <= 0)
            {
                ModelState.AddModelError(nameof(budget.Amount), "Budget amount must be greater than zero.");
            }

            try
            {
                bool duplicate = await _context.MonthlyBudgets
                    .AnyAsync(b => b.Year == budget.Year && b.Month == budget.Month && b.Id != id);

                if (duplicate)
                {
                    ModelState.AddModelError(nameof(budget.Month), $"Another budget already exists for {budget.Month}/{budget.Year}.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not check duplicate budget in database.");
                ModelState.AddModelError("", "Database connection error.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(budget);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Budget for {budget.Month}/{budget.Year} was updated successfully.";
                    return RedirectToAction(nameof(Index), new { year = budget.Year, month = budget.Month });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await BudgetExists(budget.Id)) return NotFound();
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating budget.");
                    ModelState.AddModelError("", "Database error occurred while updating.");
                }
            }

            return View(budget);
        }

        // GET: /Budgets/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || id <= 0) return NotFound();

            var budget = await _context.MonthlyBudgets.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);
            if (budget == null)
            {
                TempData["ErrorMessage"] = "Monthly budget not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(budget);
        }

        // POST: /Budgets/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var budget = await _context.MonthlyBudgets.FindAsync(id);
                if (budget != null)
                {
                    int year = budget.Year;
                    int month = budget.Month;
                    _context.MonthlyBudgets.Remove(budget);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Monthly budget for {month}/{year} deleted successfully.";
                    return RedirectToAction(nameof(Index), new { year = year, month = month });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting monthly budget.");
                TempData["ErrorMessage"] = "Database connection error. Could not delete budget.";
            }

            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Category Budget Management (Section 9)

        // GET: /Budgets/CreateCategoryBudget?year=2026&month=10
        public async Task<IActionResult> CreateCategoryBudget(int? year, int? month)
        {
            int currentYear = (year.HasValue && year.Value >= 2000 && year.Value <= 2100) ? year.Value : DateTime.Today.Year;
            int currentMonth = (month.HasValue && month.Value >= 1 && month.Value <= 12) ? month.Value : DateTime.Today.Month;

            ViewBag.Categories = await GetCategorySelectListAsync(null);

            var model = new CategoryBudget
            {
                Year = currentYear,
                Month = currentMonth,
                Amount = 5000.00m
            };

            return View(model);
        }

        // POST: /Budgets/CreateCategoryBudget
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategoryBudget([Bind("Year,Month,CategoryId,Amount")] CategoryBudget budget)
        {
            if (budget.Month < 1 || budget.Month > 12)
                ModelState.AddModelError(nameof(budget.Month), "Month must be between 1 and 12.");

            if (budget.Year < 2000 || budget.Year > 2100)
                ModelState.AddModelError(nameof(budget.Year), "Year must be between 2000 and 2100.");

            if (budget.Amount <= 0)
                ModelState.AddModelError(nameof(budget.Amount), "Budget amount must be greater than zero.");

            if (budget.CategoryId <= 0 || !await _context.Categories.AnyAsync(c => c.Id == budget.CategoryId))
                ModelState.AddModelError(nameof(budget.CategoryId), "Selected category is invalid.");

            try
            {
                bool exists = await _context.CategoryBudgets
                    .AnyAsync(cb => cb.Year == budget.Year && cb.Month == budget.Month && cb.CategoryId == budget.CategoryId);

                if (exists)
                {
                    ModelState.AddModelError(nameof(budget.CategoryId), "A budget for this category and month/year already exists.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not verify duplicate category budget.");
                ModelState.AddModelError("", "Database error occurred.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.CategoryBudgets.Add(budget);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Category budget saved successfully.";
                    return RedirectToAction(nameof(Index), new { year = budget.Year, month = budget.Month });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create category budget.");
                    ModelState.AddModelError("", "Unable to save category budget.");
                }
            }

            ViewBag.Categories = await GetCategorySelectListAsync(budget.CategoryId);
            return View(budget);
        }

        // GET: /Budgets/EditCategoryBudget/5
        public async Task<IActionResult> EditCategoryBudget(int? id)
        {
            if (id == null || id <= 0) return NotFound();

            var budget = await _context.CategoryBudgets.Include(cb => cb.Category).FirstOrDefaultAsync(cb => cb.Id == id);
            if (budget == null)
            {
                TempData["ErrorMessage"] = "Category budget not found.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categories = await GetCategorySelectListAsync(budget.CategoryId);
            return View(budget);
        }

        // POST: /Budgets/EditCategoryBudget/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategoryBudget(int id, [Bind("Id,Year,Month,CategoryId,Amount")] CategoryBudget budget)
        {
            if (id != budget.Id) return BadRequest();

            if (budget.Month < 1 || budget.Month > 12)
                ModelState.AddModelError(nameof(budget.Month), "Month must be between 1 and 12.");

            if (budget.Year < 2000 || budget.Year > 2100)
                ModelState.AddModelError(nameof(budget.Year), "Year must be between 2000 and 2100.");

            if (budget.Amount <= 0)
                ModelState.AddModelError(nameof(budget.Amount), "Budget amount must be greater than zero.");

            try
            {
                bool duplicate = await _context.CategoryBudgets
                    .AnyAsync(cb => cb.Year == budget.Year && cb.Month == budget.Month && cb.CategoryId == budget.CategoryId && cb.Id != id);

                if (duplicate)
                {
                    ModelState.AddModelError(nameof(budget.CategoryId), "Another budget already exists for this category in this month/year.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not check duplicate category budget.");
                ModelState.AddModelError("", "Database error occurred.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(budget);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Category budget updated successfully.";
                    return RedirectToAction(nameof(Index), new { year = budget.Year, month = budget.Month });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating category budget.");
                    ModelState.AddModelError("", "Database error occurred while updating category budget.");
                }
            }

            ViewBag.Categories = await GetCategorySelectListAsync(budget.CategoryId);
            return View(budget);
        }

        // POST: /Budgets/DeleteCategoryBudget/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategoryBudget(int id)
        {
            try
            {
                var budget = await _context.CategoryBudgets.FindAsync(id);
                if (budget != null)
                {
                    int year = budget.Year;
                    int month = budget.Month;
                    _context.CategoryBudgets.Remove(budget);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Category budget deleted successfully.";
                    return RedirectToAction(nameof(Index), new { year = year, month = month });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category budget.");
                TempData["ErrorMessage"] = "Could not delete category budget.";
            }

            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Helpers

        private async Task<List<SelectListItem>> GetCategorySelectListAsync(int? selectedId)
        {
            var categories = await _context.Categories
                .AsNoTracking()
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();

            return categories.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name,
                Selected = c.Id == selectedId
            }).ToList();
        }

        private async Task<bool> BudgetExists(int id)
        {
            return await _context.MonthlyBudgets.AnyAsync(e => e.Id == id);
        }

        #endregion
    }
}
