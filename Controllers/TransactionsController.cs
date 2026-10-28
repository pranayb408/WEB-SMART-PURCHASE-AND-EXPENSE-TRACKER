using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalExpenseTracker.Data;
using PersonalExpenseTracker.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PersonalExpenseTracker.Controllers
{
    public class TransactionsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<TransactionsController> _logger;

        public TransactionsController(AppDbContext context, ILogger<TransactionsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: /Transactions?year=2026&month=10&type=Expense&platform=Swiggy
        public async Task<IActionResult> Index(int? year, int? month, string? type, string? category, string? platform, string? searchTerm)
        {
            // Validate Query String parameters
            int? validYear = (year.HasValue && year.Value >= 2000 && year.Value <= 2100) ? year.Value : null;
            int? validMonth = (month.HasValue && month.Value >= 1 && month.Value <= 12) ? month.Value : null;
            string? validType = (!string.IsNullOrWhiteSpace(type) && (type == "Income" || type == "Expense")) ? type : null;

            // Start EF Core IQueryable for SQL-level filtering (avoid loading everything into memory)
            var query = _context.Transactions.AsNoTracking().AsQueryable();

            if (validYear.HasValue)
            {
                query = query.Where(t => t.Date.Year == validYear.Value);
            }

            if (validMonth.HasValue)
            {
                query = query.Where(t => t.Date.Month == validMonth.Value);
            }

            if (!string.IsNullOrEmpty(validType))
            {
                query = query.Where(t => t.Type == validType);
            }

            if (!string.IsNullOrWhiteSpace(category) && category != "All")
            {
                query = query.Where(t => t.Category == category);
            }

            if (!string.IsNullOrWhiteSpace(platform) && platform != "All")
            {
                query = query.Where(t => t.Platform == platform);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(t => t.Title.Contains(searchTerm) || 
                                         (t.Notes != null && t.Notes.Contains(searchTerm)) ||
                                         (t.OrderNumber != null && t.OrderNumber.Contains(searchTerm)));
            }

            List<Transaction> transactions;
            try
            {
                // Execute filtered query in MySQL, ordering newest first
                transactions = await query
                    .OrderByDescending(t => t.Date)
                    .ThenByDescending(t => t.Id)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not query transactions from MySQL.");
                transactions = new List<Transaction>();
                ViewBag.DatabaseOffline = true;
            }

            // Preserve filter parameters for form controls
            ViewBag.SelectedYear = validYear;
            ViewBag.SelectedMonth = validMonth;
            ViewBag.SelectedType = validType ?? "All";
            ViewBag.SelectedCategory = category;
            ViewBag.SelectedPlatform = platform;
            ViewBag.SearchTerm = searchTerm;
            ViewBag.AllCategories = TransactionCategories.GetAll();
            ViewBag.AllPlatforms = PurchasePlatforms.GetAll();
            ViewBag.GroupedPlatforms = PurchasePlatforms.GroupedPlatforms;

            ViewBag.TotalIncome = transactions.Where(t => t.Type == "Income").Sum(t => t.Amount);
            ViewBag.TotalExpense = transactions.Where(t => t.Type == "Expense").Sum(t => t.Amount);

            return View(transactions);
        }

        // GET: /Transactions/Create
        public IActionResult Create(string? type)
        {
            var model = new Transaction
            {
                Type = type == "Income" ? "Income" : "Expense",
                Date = DateTime.Today,
                Platform = "Direct / In-Store"
            };

            PopulateCategoriesViewBag(model.Type);
            return View(model);
        }

        // POST: /Transactions/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Type,Category,Amount,Date,Notes,Platform,OrderNumber,PaymentMethod")] Transaction transaction)
        {
            // Validate allowed types
            if (transaction.Type != "Income" && transaction.Type != "Expense")
            {
                ModelState.AddModelError(nameof(transaction.Type), "Transaction type must be 'Income' or 'Expense'.");
            }

            // Default platform for income or blank
            if (transaction.Type == "Income" && string.IsNullOrWhiteSpace(transaction.Platform))
            {
                transaction.Platform = "Direct / Direct Deposit";
            }
            else if (string.IsNullOrWhiteSpace(transaction.Platform))
            {
                transaction.Platform = "Direct / In-Store";
            }

            // Validate category against allowed categories
            if (!TransactionCategories.IsValid(transaction.Category, transaction.Type))
            {
                ModelState.AddModelError(nameof(transaction.Category), $"'{transaction.Category}' is not a valid category for {transaction.Type}.");
            }

            // Validate amount
            if (transaction.Amount <= 0)
            {
                ModelState.AddModelError(nameof(transaction.Amount), "Amount must be strictly greater than zero.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(transaction);
                    await _context.SaveChangesAsync();

                    // State Management via TempData
                    TempData["SuccessMessage"] = $"{transaction.Type} '{transaction.Title}' of ₹{transaction.Amount:N2} was recorded successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saving new transaction to database.");
                    ModelState.AddModelError("", "Database error occurred while saving. Please ensure MySQL is running.");
                }
            }

            PopulateCategoriesViewBag(transaction.Type);
            return View(transaction);
        }

        // GET: /Transactions/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || id <= 0)
            {
                return NotFound();
            }

            Transaction? transaction = null;
            try
            {
                transaction = await _context.Transactions.FindAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Database connection error loading transaction for edit.");
                TempData["ErrorMessage"] = "Database connection error. Could not retrieve transaction.";
                return RedirectToAction(nameof(Index));
            }

            if (transaction == null)
            {
                TempData["ErrorMessage"] = "Transaction record not found.";
                return RedirectToAction(nameof(Index));
            }

            PopulateCategoriesViewBag(transaction.Type);
            return View(transaction);
        }

        // POST: /Transactions/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Type,Category,Amount,Date,Notes,Platform,OrderNumber,PaymentMethod")] Transaction transaction)
        {
            if (id != transaction.Id)
            {
                return BadRequest();
            }

            // Validate allowed types
            if (transaction.Type != "Income" && transaction.Type != "Expense")
            {
                ModelState.AddModelError(nameof(transaction.Type), "Transaction type must be 'Income' or 'Expense'.");
            }

            if (string.IsNullOrWhiteSpace(transaction.Platform))
            {
                transaction.Platform = transaction.Type == "Income" ? "Direct / Direct Deposit" : "Direct / In-Store";
            }

            // Validate category against allowed options
            if (!TransactionCategories.IsValid(transaction.Category, transaction.Type))
            {
                ModelState.AddModelError(nameof(transaction.Category), $"'{transaction.Category}' is not a valid category for {transaction.Type}.");
            }

            // Validate amount
            if (transaction.Amount <= 0)
            {
                ModelState.AddModelError(nameof(transaction.Amount), "Amount must be greater than zero.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(transaction);
                    await _context.SaveChangesAsync();

                    // State Management via TempData
                    TempData["SuccessMessage"] = $"Transaction '{transaction.Title}' updated successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await TransactionExists(transaction.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating transaction in database.");
                    ModelState.AddModelError("", "Unable to save updates to database. Please check database connectivity.");
                }
            }

            PopulateCategoriesViewBag(transaction.Type);
            return View(transaction);
        }

        // GET: /Transactions/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || id <= 0)
            {
                return NotFound();
            }

            Transaction? transaction = null;
            try
            {
                transaction = await _context.Transactions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Database connection error loading transaction for deletion.");
                TempData["ErrorMessage"] = "Database connection error. Could not retrieve transaction.";
                return RedirectToAction(nameof(Index));
            }

            if (transaction == null)
            {
                TempData["ErrorMessage"] = "Transaction record was not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(transaction);
        }

        // POST: /Transactions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var transaction = await _context.Transactions.FindAsync(id);
                if (transaction != null)
                {
                    _context.Transactions.Remove(transaction);
                    await _context.SaveChangesAsync();

                    // State Management via TempData
                    TempData["SuccessMessage"] = $"Transaction '{transaction.Title}' was successfully deleted.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Transaction could not be found or has already been deleted.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting transaction from database.");
                TempData["ErrorMessage"] = "Database error. Could not delete transaction.";
            }

            return RedirectToAction(nameof(Index));
        }

        private void PopulateCategoriesViewBag(string type)
        {
            ViewBag.ExpenseCategories = TransactionCategories.ExpenseCategories;
            ViewBag.IncomeCategories = TransactionCategories.IncomeCategories;
            ViewBag.CurrentCategories = type == "Income" 
                ? TransactionCategories.IncomeCategories 
                : TransactionCategories.ExpenseCategories;
            ViewBag.Platforms = PurchasePlatforms.GetAll();
            ViewBag.GroupedPlatforms = PurchasePlatforms.GroupedPlatforms;
            ViewBag.PaymentMethods = PaymentMethods.All;
        }

        private async Task<bool> TransactionExists(int id)
        {
            try
            {
                return await _context.Transactions.AnyAsync(e => e.Id == id);
            }
            catch
            {
                return false;
            }
        }
    }
}
