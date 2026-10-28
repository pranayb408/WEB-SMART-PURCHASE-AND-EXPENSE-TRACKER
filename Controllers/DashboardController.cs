using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalExpenseTracker.Data;
using PersonalExpenseTracker.Models;
using PersonalExpenseTracker.Services;
using PersonalExpenseTracker.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace PersonalExpenseTracker.Controllers
{
    public class DashboardController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IFinancialAccountingService _accounting;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(
            AppDbContext context, 
            IFinancialAccountingService accounting,
            ILogger<DashboardController> logger)
        {
            _context = context;
            _accounting = accounting;
            _logger = logger;
        }

        // GET: /Dashboard/Index
        public async Task<IActionResult> Index(
            int? month, 
            int? year, 
            int? platformId, 
            int? categoryId, 
            string? orderStatus)
        {
            // --- State Management via Session ---
            int? storedMonth = HttpContext.Session.GetInt32("SelectedMonth");
            int? storedYear = HttpContext.Session.GetInt32("SelectedYear");

            // Validate provided parameters or restore from session
            int currentMonth = (month.HasValue && month.Value >= 1 && month.Value <= 12)
                ? month.Value
                : (storedMonth.HasValue && storedMonth.Value >= 1 && storedMonth.Value <= 12 ? storedMonth.Value : DateTime.Today.Month);

            int currentYear = (year.HasValue && year.Value >= 2000 && year.Value <= 2100)
                ? year.Value
                : (storedYear.HasValue && storedYear.Value >= 2000 && storedYear.Value <= 2100 ? storedYear.Value : DateTime.Today.Year);

            // Save active selection back into Session
            HttpContext.Session.SetInt32("SelectedMonth", currentMonth);
            HttpContext.Session.SetInt32("SelectedYear", currentYear);

            // Consistent inclusive-start and exclusive-end date ranges (Section 8)
            DateTime startOfMonth = new DateTime(currentYear, currentMonth, 1);
            DateTime startOfNextMonth = startOfMonth.AddMonths(1);

            DateTime startOfYear = new DateTime(currentYear, 1, 1);
            DateTime startOfNextYear = startOfYear.AddYears(1);

            try
            {
                // 1. Query standalone transactions for selected period
                var txQuery = _context.Transactions
                    .AsNoTracking()
                    .Where(t => t.Date >= startOfMonth && t.Date < startOfNextMonth);

                if (platformId.HasValue && platformId.Value > 0)
                {
                    var plat = await _context.Platforms.FindAsync(platformId.Value);
                    if (plat != null)
                    {
                        txQuery = txQuery.Where(t => t.Platform == plat.Name);
                    }
                }

                if (categoryId.HasValue && categoryId.Value > 0)
                {
                    var cat = await _context.Categories.FindAsync(categoryId.Value);
                    if (cat != null)
                    {
                        txQuery = txQuery.Where(t => t.Category == cat.Name);
                    }
                }

                var monthTransactions = await txQuery.ToListAsync();

                // 2. Query purchase orders with items for selected period
                var poQuery = _context.PurchaseOrders
                    .AsNoTracking()
                    .Include(p => p.Platform)
                    .Include(p => p.Items)
                        .ThenInclude(i => i.Category)
                    .Where(p => p.OrderDate >= startOfMonth && p.OrderDate < startOfNextMonth);

                if (platformId.HasValue && platformId.Value > 0)
                {
                    poQuery = poQuery.Where(p => p.PlatformId == platformId.Value);
                }

                if (categoryId.HasValue && categoryId.Value > 0)
                {
                    poQuery = poQuery.Where(p => p.Items.Any(i => i.CategoryId == categoryId.Value));
                }

                if (!string.IsNullOrWhiteSpace(orderStatus) && orderStatus != "All")
                {
                    poQuery = poQuery.Where(p => p.OrderStatus == orderStatus);
                }

                var monthOrders = await poQuery.ToListAsync();

                // 3. Yearly Spending Calculation (Section 8: Yearly spending)
                var yearlyStandaloneAmounts = await _context.Transactions
                    .AsNoTracking()
                    .Where(t => t.Date >= startOfYear && t.Date < startOfNextYear && t.Type == "Expense")
                    .Select(t => t.Amount)
                    .ToListAsync();
                decimal yearlyStandalone = yearlyStandaloneAmounts.Sum();

                var yearlyCompletedOrders = await _context.PurchaseOrders
                    .AsNoTracking()
                    .Where(p => p.OrderDate >= startOfYear && p.OrderDate < startOfNextYear && p.OrderStatus == OrderStatus.Completed)
                    .Select(p => new { p.FinalBillAmount, p.RefundAmount })
                    .ToListAsync();

                decimal yearlyOrderSpending = yearlyCompletedOrders.Sum(p => p.FinalBillAmount - p.RefundAmount);
                decimal yearlySpending = Math.Round(yearlyStandalone + yearlyOrderSpending, 2, MidpointRounding.AwayFromZero);

                // Apply rigorous financial accounting business rules
                decimal standaloneIncome = _accounting.CalculateStandaloneIncome(monthTransactions);
                decimal standaloneExpenses = _accounting.CalculateStandaloneExpenses(monthTransactions);
                decimal netPurchaseSpending = _accounting.CalculateNetPurchaseSpending(monthOrders);
                decimal pendingPurchases = _accounting.CalculatePendingPurchases(monthOrders);
                decimal cancelledPurchases = _accounting.CalculateCancelledPurchases(monthOrders);
                decimal grossPurchases = monthOrders.Where(o => o.OrderStatus == OrderStatus.Completed).Sum(o => o.FinalBillAmount);
                decimal refundTotals = monthOrders.Sum(o => o.RefundAmount);

                decimal totalExpenses = _accounting.CalculateTotalExpenses(standaloneExpenses, netPurchaseSpending);

                // Query Monthly Budget for selected period
                var budgetRecord = await _context.MonthlyBudgets
                    .AsNoTracking()
                    .FirstOrDefaultAsync(b => b.Year == currentYear && b.Month == currentMonth);

                decimal monthlyBudgetAmount = budgetRecord?.Amount ?? 0m;

                // Recent transactions for the selected month (newest first)
                var recentTransactions = monthTransactions
                    .OrderByDescending(t => t.Date)
                    .ThenByDescending(t => t.Id)
                    .Take(6)
                    .ToList();

                var recentOrders = monthOrders
                    .OrderByDescending(p => p.OrderDate)
                    .ThenByDescending(p => p.Id)
                    .Take(5)
                    .ToList();

                // Multi-category breakdown combining standalone expenses + order items (proportional attribution)
                var categoryExpenses = _accounting.CalculateCategoryWiseSpending(
                    monthTransactions.Where(t => t.Type == "Expense"), 
                    monthOrders);

                // Platform breakdown from completed purchase orders (or fallback to transaction platforms)
                var platformExpenses = _accounting.CalculatePlatformWiseSpending(monthOrders);
                if (!platformExpenses.Any())
                {
                    platformExpenses = monthTransactions
                        .Where(t => t.Type == "Expense")
                        .GroupBy(t => string.IsNullOrWhiteSpace(t.Platform) ? "Direct / In-Store" : t.Platform)
                        .Select(g => new { Platform = g.Key, Total = g.Sum(t => t.Amount) })
                        .OrderByDescending(g => g.Total)
                        .ToDictionary(g => g.Platform, g => g.Total);
                }

                var topPlatformEntry = platformExpenses.FirstOrDefault();
                string? topPlatform = topPlatformEntry.Key;
                decimal topPlatformAmount = topPlatformEntry.Value;

                // 6-Month Historical Trends
                var trendMonths = new List<string>();
                var trendIncomes = new List<decimal>();
                var trendExpenses = new List<decimal>();

                var startDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-5);
                var histTransactions = await _context.Transactions
                    .AsNoTracking()
                    .Where(t => t.Date >= startDate)
                    .Select(t => new { t.Date, t.Type, t.Amount })
                    .ToListAsync();

                var histOrders = await _context.PurchaseOrders
                    .AsNoTracking()
                    .Where(p => p.OrderDate >= startDate && p.OrderStatus == OrderStatus.Completed)
                    .Select(p => new { p.OrderDate, p.FinalBillAmount, p.RefundAmount })
                    .ToListAsync();

                for (int i = 5; i >= 0; i--)
                {
                    var date = DateTime.Today.AddMonths(-i);
                    trendMonths.Add(date.ToString("MMM yyyy", CultureInfo.InvariantCulture));

                    decimal hInc = histTransactions
                        .Where(t => t.Date.Year == date.Year && t.Date.Month == date.Month && t.Type == "Income")
                        .Sum(t => t.Amount);

                    decimal hStandaloneExp = histTransactions
                        .Where(t => t.Date.Year == date.Year && t.Date.Month == date.Month && t.Type == "Expense")
                        .Sum(t => t.Amount);

                    decimal hOrderExp = histOrders
                        .Where(o => o.OrderDate.Year == date.Year && o.OrderDate.Month == date.Month)
                        .Sum(o => o.FinalBillAmount - o.RefundAmount);

                    trendIncomes.Add(hInc);
                    trendExpenses.Add(hStandaloneExp + hOrderExp);
                }

                // Available filter options
                var allPlatforms = await _context.Platforms.AsNoTracking().OrderBy(p => p.Name).ToListAsync();
                var allCategories = await _context.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync();

                var platOptions = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>
                {
                    new() { Value = "", Text = "All Platforms" }
                };
                platOptions.AddRange(allPlatforms.Select(p => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = p.Name,
                    Selected = p.Id == platformId
                }));

                var catOptions = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>
                {
                    new() { Value = "", Text = "All Categories" }
                };
                catOptions.AddRange(allCategories.Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name,
                    Selected = c.Id == categoryId
                }));

                var statusOptions = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>
                {
                    new() { Value = "", Text = "All Statuses" }
                };
                statusOptions.AddRange(OrderStatus.All.Select(s => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = s,
                    Text = s,
                    Selected = s == orderStatus
                }));

                var viewModel = new DashboardViewModel
                {
                    SelectedMonth = currentMonth,
                    SelectedYear = currentYear,
                    SelectedPlatformId = platformId,
                    SelectedCategoryId = categoryId,
                    SelectedOrderStatus = orderStatus,
                    StandaloneIncome = standaloneIncome,
                    StandaloneExpenses = standaloneExpenses,
                    NetPurchaseSpending = netPurchaseSpending,
                    GrossPurchaseSpending = grossPurchases,
                    RefundTotals = refundTotals,
                    PendingPurchasesTotal = pendingPurchases,
                    CancelledPurchasesTotal = cancelledPurchases,
                    YearlySpending = yearlySpending,
                    MonthlyBudgetAmount = monthlyBudgetAmount,
                    RecentTransactions = recentTransactions,
                    RecentPurchaseOrders = recentOrders,
                    CategoryExpenses = categoryExpenses,
                    PlatformExpenses = platformExpenses,
                    TopSpendingPlatform = topPlatform,
                    TopPlatformAmount = topPlatformAmount,
                    TrendMonths = trendMonths,
                    TrendIncomes = trendIncomes,
                    TrendExpenses = trendExpenses,
                    AvailablePlatforms = platOptions,
                    AvailableCategories = catOptions,
                    AvailableStatuses = statusOptions
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MySQL database query failed on Dashboard.");
                ViewBag.DatabaseOffline = true;
                ViewBag.DatabaseErrorMessage = "MySQL connection not established on localhost:3306. Please ensure MySQL is running (e.g. XAMPP Control Panel) to view and persist live records.";
                ViewBag.ExceptionDetail = ex.ToString();

                var dynamicFallbackMonths = new List<string>();
                for (int i = 5; i >= 0; i--)
                {
                    dynamicFallbackMonths.Add(DateTime.Today.AddMonths(-i).ToString("MMM yyyy", CultureInfo.InvariantCulture));
                }

                var fallback = new DashboardViewModel
                {
                    SelectedMonth = currentMonth,
                    SelectedYear = currentYear,
                    TotalIncome = 0m,
                    TotalExpenses = 0m,
                    MonthlyBudgetAmount = 0m,
                    RecentTransactions = new List<Transaction>(),
                    TrendMonths = dynamicFallbackMonths,
                    TrendIncomes = new List<decimal> { 0, 0, 0, 0, 0, 0 },
                    TrendExpenses = new List<decimal> { 0, 0, 0, 0, 0, 0 }
                };

                return View(fallback);
            }
        }
    }
}
