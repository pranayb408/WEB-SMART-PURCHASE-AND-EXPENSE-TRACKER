using System;
using System.Collections.Generic;
using PersonalExpenseTracker.Models;

namespace PersonalExpenseTracker.ViewModels
{
    public class DashboardViewModel
    {
        public int SelectedMonth { get; set; } = DateTime.Today.Month;
        public int SelectedYear { get; set; } = DateTime.Today.Year;

        // Core Financial Metrics (Per Business Accounting Rules)
        public decimal StandaloneIncome { get; set; }
        public decimal StandaloneExpenses { get; set; }
        public decimal NetPurchaseSpending { get; set; }
        public decimal GrossPurchaseSpending { get; set; }
        public decimal RefundTotals { get; set; }
        public decimal PendingPurchasesTotal { get; set; }
        public decimal CancelledPurchasesTotal { get; set; }
        public decimal YearlySpending { get; set; }

        // Dynamic Filtering
        public int? SelectedPlatformId { get; set; }
        public int? SelectedCategoryId { get; set; }
        public string? SelectedOrderStatus { get; set; }

        public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> AvailablePlatforms { get; set; } = new();
        public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> AvailableCategories { get; set; } = new();
        public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> AvailableStatuses { get; set; } = new();

        public decimal TotalIncome
        {
            get => StandaloneIncome;
            set => StandaloneIncome = value;
        }

        // Total expenses = standalone expenses plus net purchase spending
        public decimal TotalExpenses
        {
            get => Math.Round(StandaloneExpenses + NetPurchaseSpending, 2, MidpointRounding.AwayFromZero);
            set
            {
                // If set directly, update StandaloneExpenses (when net purchases is 0)
                if (NetPurchaseSpending == 0m)
                {
                    StandaloneExpenses = value;
                }
            }
        }

        public decimal MonthlyBudgetAmount { get; set; }

        // Required Formulas
        // 1. Net balance = standalone income minus total expenses
        public decimal NetBalance => Math.Round(TotalIncome - TotalExpenses, 2, MidpointRounding.AwayFromZero);

        // 2. Budget remaining = Monthly budget - Total expenses
        public decimal BudgetRemaining => Math.Round(MonthlyBudgetAmount - TotalExpenses, 2, MidpointRounding.AwayFromZero);

        // 3. Budget usage percentage = Total expenses / Monthly budget * 100 (avoids division by zero)
        public double BudgetUsagePercentage => HasBudget 
            ? (double)Math.Round((TotalExpenses / MonthlyBudgetAmount) * 100, 1) 
            : 0.0;

        public bool HasBudget => MonthlyBudgetAmount > 0;
        public bool IsOverBudget => HasBudget && TotalExpenses > MonthlyBudgetAmount;
        public decimal OverBudgetAmount => IsOverBudget ? (TotalExpenses - MonthlyBudgetAmount) : 0m;

        // Progress bar styling
        public string ProgressBarClass
        {
            get
            {
                if (!HasBudget) return "bg-secondary";
                if (IsOverBudget) return "bg-danger";
                if (BudgetUsagePercentage >= 80) return "bg-warning";
                return "bg-success";
            }
        }

        // Transactions for the selected month
        public List<Transaction> RecentTransactions { get; set; } = new List<Transaction>();
        public List<PurchaseOrder> RecentPurchaseOrders { get; set; } = new List<PurchaseOrder>();

        // Analytics
        public Dictionary<string, decimal> CategoryExpenses { get; set; } = new Dictionary<string, decimal>();
        public Dictionary<string, decimal> PlatformExpenses { get; set; } = new Dictionary<string, decimal>();
        public string? TopSpendingPlatform { get; set; }
        public decimal TopPlatformAmount { get; set; }
        public List<string> TrendMonths { get; set; } = new List<string>();
        public List<decimal> TrendIncomes { get; set; } = new List<decimal>();
        public List<decimal> TrendExpenses { get; set; } = new List<decimal>();
    }
}
