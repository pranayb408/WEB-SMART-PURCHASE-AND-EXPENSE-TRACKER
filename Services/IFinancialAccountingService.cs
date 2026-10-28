using System.Collections.Generic;
using PersonalExpenseTracker.Models;

namespace PersonalExpenseTracker.Services
{
    public interface IFinancialAccountingService
    {
        /// <summary>
        /// Standalone income = sum of eligible Income transactions.
        /// </summary>
        decimal CalculateStandaloneIncome(IEnumerable<Transaction> transactions);

        /// <summary>
        /// Standalone expenses = sum of eligible Expense transactions.
        /// </summary>
        decimal CalculateStandaloneExpenses(IEnumerable<Transaction> transactions);

        /// <summary>
        /// Net purchase spending = completed purchase bills minus recorded refunds.
        /// Excludes pending and cancelled orders.
        /// </summary>
        decimal CalculateNetPurchaseSpending(IEnumerable<PurchaseOrder> orders);

        /// <summary>
        /// Total pending purchase commitments.
        /// </summary>
        decimal CalculatePendingPurchases(IEnumerable<PurchaseOrder> orders);

        /// <summary>
        /// Total value of cancelled orders (excluded from realized expenses).
        /// </summary>
        decimal CalculateCancelledPurchases(IEnumerable<PurchaseOrder> orders);

        /// <summary>
        /// Total expenses = standalone expenses plus net purchase spending.
        /// </summary>
        decimal CalculateTotalExpenses(decimal standaloneExpenses, decimal netPurchaseSpending);

        /// <summary>
        /// Net balance = standalone income minus total expenses.
        /// </summary>
        decimal CalculateNetBalance(decimal standaloneIncome, decimal totalExpenses);

        /// <summary>
        /// Combines itemized purchase order items and standalone expenses per category.
        /// </summary>
        Dictionary<string, decimal> CalculateCategoryWiseSpending(
            IEnumerable<Transaction> standaloneExpenses, 
            IEnumerable<PurchaseOrder> completedOrders);

        /// <summary>
        /// Groups completed purchase orders by platform.
        /// </summary>
        Dictionary<string, decimal> CalculatePlatformWiseSpending(IEnumerable<PurchaseOrder> completedOrders);

        /// <summary>
        /// Compares budget against total spending.
        /// </summary>
        BudgetComparisonResult CalculateBudgetComparison(decimal budgetAmount, decimal totalSpending);
    }

    public class BudgetComparisonResult
    {
        public decimal BudgetAmount { get; set; }
        public decimal TotalSpending { get; set; }
        public decimal Remaining => BudgetAmount - TotalSpending;
        public bool HasBudget => BudgetAmount > 0m;
        public bool IsOverBudget => HasBudget && TotalSpending > BudgetAmount;
        public decimal OverBudgetAmount => IsOverBudget ? (TotalSpending - BudgetAmount) : 0m;
        public double UsagePercentage => HasBudget 
            ? System.Math.Round((double)(TotalSpending / BudgetAmount) * 100, 1) 
            : 0.0;
        public string ProgressBarClass
        {
            get
            {
                if (!HasBudget) return "bg-secondary";
                if (IsOverBudget) return "bg-danger";
                if (UsagePercentage >= 80) return "bg-warning";
                return "bg-success";
            }
        }
    }
}

