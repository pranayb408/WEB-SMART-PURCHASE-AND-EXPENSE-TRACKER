using System;
using System.Collections.Generic;
using PersonalExpenseTracker.Models;

namespace PersonalExpenseTracker.ViewModels
{
    public class ReportsViewModel
    {
        public string ReportType { get; set; } = "Monthly"; // "Monthly" or "Yearly"
        public int SelectedYear { get; set; } = DateTime.Today.Year;
        public int SelectedMonth { get; set; } = DateTime.Today.Month;

        // Financial Totals
        public decimal TotalIncome { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal NetSavings => TotalIncome - TotalExpenses;

        public double SavingsRatePercentage => TotalIncome > 0
            ? Math.Round((double)((NetSavings / TotalIncome) * 100), 1)
            : 0.0;

        public int TotalTransactionsCount { get; set; }
        public int TotalPurchasesCount { get; set; }
        public decimal AveragePurchaseAmount => TotalPurchasesCount > 0
            ? Math.Round(TotalExpenses / TotalPurchasesCount, 2)
            : 0m;

        // Platform-wise Breakdown
        public List<PlatformReportItem> PlatformBreakdown { get; set; } = new();

        // Category-wise Breakdown
        public List<CategoryReportItem> CategoryBreakdown { get; set; } = new();

        // Yearly 12-Month Summary (populated when ReportType == "Yearly")
        public List<MonthlyReportSummaryItem> YearlyMonthlySummaries { get; set; } = new();

        // Highlights
        public string? PeakExpenseMonth { get; set; }
        public decimal PeakExpenseAmount { get; set; }
        public string? LowestExpenseMonth { get; set; }
        public decimal LowestExpenseAmount { get; set; }
        public string? TopSpendingPlatform { get; set; }
        public decimal TopPlatformAmount { get; set; }
        public string? BiggestPurchaseTitle { get; set; }
        public decimal BiggestPurchaseAmount { get; set; }
        public string? BiggestPurchasePlatform { get; set; }

        // Filtered transactions for printable report table
        public List<Transaction> ReportTransactions { get; set; } = new();
    }

    public class PlatformReportItem
    {
        public string Platform { get; set; } = string.Empty;
        public decimal TotalSpent { get; set; }
        public int OrderCount { get; set; }
        public double PercentageOfTotal { get; set; }
        public decimal AverageOrderValue => OrderCount > 0 ? Math.Round(TotalSpent / OrderCount, 2) : 0m;
        public string TopCategory { get; set; } = string.Empty;
    }

    public class CategoryReportItem
    {
        public string Category { get; set; } = string.Empty;
        public decimal TotalSpent { get; set; }
        public int ItemCount { get; set; }
        public double PercentageOfTotal { get; set; }
    }

    public class MonthlyReportSummaryItem
    {
        public int MonthNumber { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public decimal Income { get; set; }
        public decimal Expense { get; set; }
        public decimal NetBalance => Income - Expense;
        public decimal Budget { get; set; }
        public string? TopPlatform { get; set; }
    }
}

