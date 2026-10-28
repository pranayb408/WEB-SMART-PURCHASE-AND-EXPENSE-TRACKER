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
    public class ReportsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IFinancialAccountingService _accounting;
        private readonly ILogger<ReportsController> _logger;

        public ReportsController(
            AppDbContext context, 
            IFinancialAccountingService accounting,
            ILogger<ReportsController> logger)
        {
            _context = context;
            _accounting = accounting;
            _logger = logger;
        }

        // GET: /Reports?reportType=Monthly&year=2026&month=10
        public async Task<IActionResult> Index(string? reportType, int? year, int? month)
        {
            string type = string.Equals(reportType, "Yearly", StringComparison.OrdinalIgnoreCase) ? "Yearly" : "Monthly";

            // State management via session fallback
            int currentYear = (year.HasValue && year.Value >= 2000 && year.Value <= 2100)
                ? year.Value
                : (HttpContext.Session.GetInt32("SelectedYear") ?? DateTime.Today.Year);

            int currentMonth = (month.HasValue && month.Value >= 1 && month.Value <= 12)
                ? month.Value
                : (HttpContext.Session.GetInt32("SelectedMonth") ?? DateTime.Today.Month);

            HttpContext.Session.SetInt32("SelectedYear", currentYear);
            HttpContext.Session.SetInt32("SelectedMonth", currentMonth);

            var model = new ReportsViewModel
            {
                ReportType = type,
                SelectedYear = currentYear,
                SelectedMonth = currentMonth
            };

            try
            {
                if (type == "Yearly")
                {
                    await PopulateYearlyReport(model, currentYear);
                }
                else
                {
                    await PopulateMonthlyReport(model, currentYear, currentMonth);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to build financial reports from database.");
                ViewBag.DatabaseOffline = true;
            }

            ViewBag.AvailableYears = Enumerable.Range(DateTime.Today.Year - 4, 10).ToList();
            ViewBag.Months = DateTimeFormatInfo.InvariantInfo.MonthNames.Take(12)
                .Select((name, idx) => new { Index = idx + 1, Name = name })
                .ToList();

            return View(model);
        }

        private async Task PopulateMonthlyReport(ReportsViewModel model, int year, int month)
        {
            DateTime startOfMonth = new DateTime(year, month, 1);
            DateTime startOfNextMonth = startOfMonth.AddMonths(1);

            var transactions = await _context.Transactions
                .AsNoTracking()
                .Where(t => t.Date >= startOfMonth && t.Date < startOfNextMonth)
                .OrderByDescending(t => t.Date)
                .ThenByDescending(t => t.Id)
                .ToListAsync();

            var orders = await _context.PurchaseOrders
                .AsNoTracking()
                .Include(p => p.Items)
                .Where(p => p.OrderDate >= startOfMonth && p.OrderDate < startOfNextMonth)
                .OrderByDescending(p => p.OrderDate)
                .ThenByDescending(p => p.Id)
                .ToListAsync();

            model.ReportTransactions = transactions;
            model.TotalTransactionsCount = transactions.Count + orders.Count;

            decimal standaloneIncome = _accounting.CalculateStandaloneIncome(transactions);
            decimal standaloneExpenses = _accounting.CalculateStandaloneExpenses(transactions);
            decimal netPurchaseSpending = _accounting.CalculateNetPurchaseSpending(orders);

            model.TotalIncome = standaloneIncome;
            model.TotalExpenses = _accounting.CalculateTotalExpenses(standaloneExpenses, netPurchaseSpending);
            model.TotalPurchasesCount = orders.Count(o => o.Status == OrderStatus.Completed) + 
                                        transactions.Count(t => t.Type == "Expense");

            // Category Breakdown combining standalone expenses & purchase orders
            var categorySpending = _accounting.CalculateCategoryWiseSpending(
                transactions.Where(t => t.Type == "Expense"), 
                orders);

            foreach (var kv in categorySpending)
            {
                double pct = model.TotalExpenses > 0 ? Math.Round((double)(kv.Value / model.TotalExpenses) * 100, 1) : 0;
                model.CategoryBreakdown.Add(new CategoryReportItem
                {
                    Category = kv.Key,
                    TotalSpent = kv.Value,
                    PercentageOfTotal = pct
                });
            }

            // Platform Breakdown combining completed orders & standalone transactions
            var platformSpending = _accounting.CalculatePlatformWiseSpending(orders);
            foreach (var tx in transactions.Where(t => t.Type == "Expense"))
            {
                string plat = string.IsNullOrWhiteSpace(tx.Platform) ? "Direct / In-Store" : tx.Platform;
                platformSpending.TryGetValue(plat, out decimal cur);
                platformSpending[plat] = cur + tx.Amount;
            }

            foreach (var kv in platformSpending.OrderByDescending(kv => kv.Value))
            {
                int orderCount = orders.Count(o => (o.PlatformName == kv.Key || o.Platform?.Name == kv.Key) && o.Status == OrderStatus.Completed) +
                                 transactions.Count(t => t.Platform == kv.Key && t.Type == "Expense");
                double pct = model.TotalExpenses > 0 ? Math.Round((double)(kv.Value / model.TotalExpenses) * 100, 1) : 0;

                model.PlatformBreakdown.Add(new PlatformReportItem
                {
                    Platform = kv.Key,
                    TotalSpent = kv.Value,
                    OrderCount = orderCount,
                    PercentageOfTotal = pct
                });
            }

            var topPlatform = model.PlatformBreakdown.FirstOrDefault();
            model.TopSpendingPlatform = topPlatform?.Platform;
            model.TopPlatformAmount = topPlatform?.TotalSpent ?? 0m;

            // Highlights
            var biggestOrder = orders
                .Where(o => o.Status == OrderStatus.Completed)
                .OrderByDescending(o => o.TotalAmount)
                .FirstOrDefault();

            var biggestTx = transactions
                .Where(t => t.Type == "Expense")
                .OrderByDescending(t => t.Amount)
                .FirstOrDefault();

            if (biggestOrder != null && (biggestTx == null || biggestOrder.TotalAmount >= biggestTx.Amount))
            {
                model.BiggestPurchaseTitle = $"Order {biggestOrder.OrderNumber}";
                model.BiggestPurchaseAmount = biggestOrder.TotalAmount;
                model.BiggestPurchasePlatform = biggestOrder.PlatformName;
            }
            else if (biggestTx != null)
            {
                model.BiggestPurchaseTitle = biggestTx.Title;
                model.BiggestPurchaseAmount = biggestTx.Amount;
                model.BiggestPurchasePlatform = biggestTx.Platform;
            }
        }

        private async Task PopulateYearlyReport(ReportsViewModel model, int year)
        {
            DateTime startOfYear = new DateTime(year, 1, 1);
            DateTime startOfNextYear = startOfYear.AddYears(1);

            var yearTransactions = await _context.Transactions
                .AsNoTracking()
                .Where(t => t.Date >= startOfYear && t.Date < startOfNextYear)
                .OrderByDescending(t => t.Date)
                .ThenByDescending(t => t.Id)
                .ToListAsync();

            var yearOrders = await _context.PurchaseOrders
                .AsNoTracking()
                .Include(p => p.Items)
                .Where(p => p.OrderDate >= startOfYear && p.OrderDate < startOfNextYear)
                .OrderByDescending(p => p.OrderDate)
                .ThenByDescending(p => p.Id)
                .ToListAsync();

            var yearBudgets = await _context.MonthlyBudgets
                .AsNoTracking()
                .Where(b => b.Year == year)
                .ToListAsync();

            model.ReportTransactions = yearTransactions;
            model.TotalTransactionsCount = yearTransactions.Count + yearOrders.Count;

            decimal standaloneIncome = _accounting.CalculateStandaloneIncome(yearTransactions);
            decimal standaloneExpenses = _accounting.CalculateStandaloneExpenses(yearTransactions);
            decimal netPurchaseSpending = _accounting.CalculateNetPurchaseSpending(yearOrders);

            model.TotalIncome = standaloneIncome;
            model.TotalExpenses = _accounting.CalculateTotalExpenses(standaloneExpenses, netPurchaseSpending);
            model.TotalPurchasesCount = yearOrders.Count(o => o.Status == OrderStatus.Completed) + 
                                        yearTransactions.Count(t => t.Type == "Expense");

            // 12-Month Table
            var monthlySummaries = new List<MonthlyReportSummaryItem>();
            string[] monthNames = DateTimeFormatInfo.InvariantInfo.MonthNames;

            decimal peakExp = decimal.MinValue;
            string peakMonth = string.Empty;
            decimal lowExp = decimal.MaxValue;
            string lowMonth = string.Empty;
            bool hasAnyExp = false;

            for (int m = 1; m <= 12; m++)
            {
                var mTx = yearTransactions.Where(t => t.Date.Month == m).ToList();
                var mOrders = yearOrders.Where(o => o.OrderDate.Month == m).ToList();

                decimal mInc = _accounting.CalculateStandaloneIncome(mTx);
                decimal mStandaloneExp = _accounting.CalculateStandaloneExpenses(mTx);
                decimal mNetOrderExp = _accounting.CalculateNetPurchaseSpending(mOrders);
                decimal mExp = _accounting.CalculateTotalExpenses(mStandaloneExp, mNetOrderExp);
                decimal mBudget = yearBudgets.FirstOrDefault(b => b.Month == m)?.Amount ?? 0m;

                var mPlatformSpending = _accounting.CalculatePlatformWiseSpending(mOrders);
                string? topPlatform = mPlatformSpending.FirstOrDefault().Key;

                string mName = monthNames[m - 1];
                monthlySummaries.Add(new MonthlyReportSummaryItem
                {
                    MonthNumber = m,
                    MonthName = mName,
                    Income = mInc,
                    Expense = mExp,
                    Budget = mBudget,
                    TopPlatform = topPlatform
                });

                if (mExp > 0)
                {
                    hasAnyExp = true;
                    if (mExp > peakExp)
                    {
                        peakExp = mExp;
                        peakMonth = mName;
                    }
                    if (mExp < lowExp)
                    {
                        lowExp = mExp;
                        lowMonth = mName;
                    }
                }
            }

            model.YearlyMonthlySummaries = monthlySummaries;
            if (hasAnyExp)
            {
                model.PeakExpenseMonth = peakMonth;
                model.PeakExpenseAmount = peakExp;
                model.LowestExpenseMonth = lowMonth;
                model.LowestExpenseAmount = lowExp;
            }

            // Platform breakdown for the full year
            var platformSpending = _accounting.CalculatePlatformWiseSpending(yearOrders);
            foreach (var tx in yearTransactions.Where(t => t.Type == "Expense"))
            {
                string plat = string.IsNullOrWhiteSpace(tx.Platform) ? "Direct / In-Store" : tx.Platform;
                platformSpending.TryGetValue(plat, out decimal cur);
                platformSpending[plat] = cur + tx.Amount;
            }

            foreach (var kv in platformSpending.OrderByDescending(kv => kv.Value))
            {
                int orderCount = yearOrders.Count(o => (o.PlatformName == kv.Key || o.Platform?.Name == kv.Key) && o.Status == OrderStatus.Completed) +
                                 yearTransactions.Count(t => t.Platform == kv.Key && t.Type == "Expense");
                double pct = model.TotalExpenses > 0 ? Math.Round((double)(kv.Value / model.TotalExpenses) * 100, 1) : 0;

                model.PlatformBreakdown.Add(new PlatformReportItem
                {
                    Platform = kv.Key,
                    TotalSpent = kv.Value,
                    OrderCount = orderCount,
                    PercentageOfTotal = pct
                });
            }

            var topPlatformEntry = model.PlatformBreakdown.FirstOrDefault();
            model.TopSpendingPlatform = topPlatformEntry?.Platform;
            model.TopPlatformAmount = topPlatformEntry?.TotalSpent ?? 0m;

            // Category breakdown for full year
            var categorySpending = _accounting.CalculateCategoryWiseSpending(
                yearTransactions.Where(t => t.Type == "Expense"), 
                yearOrders);

            foreach (var kv in categorySpending)
            {
                double pct = model.TotalExpenses > 0 ? Math.Round((double)(kv.Value / model.TotalExpenses) * 100, 1) : 0;
                model.CategoryBreakdown.Add(new CategoryReportItem
                {
                    Category = kv.Key,
                    TotalSpent = kv.Value,
                    PercentageOfTotal = pct
                });
            }
        }
    }
}
