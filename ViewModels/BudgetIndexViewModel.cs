using System;
using System.Collections.Generic;
using PersonalExpenseTracker.Models;

namespace PersonalExpenseTracker.ViewModels
{
    public class CategoryBudgetProgressViewModel
    {
        public int Id { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal BudgetAmount { get; set; }
        public decimal ActualSpending { get; set; }

        public decimal RemainingBudget => Math.Round(BudgetAmount - ActualSpending, 2, MidpointRounding.AwayFromZero);
        public bool IsOverBudget => ActualSpending > BudgetAmount;
        public decimal OverBudgetAmount => IsOverBudget ? Math.Round(ActualSpending - BudgetAmount, 2, MidpointRounding.AwayFromZero) : 0m;

        // Handles missing budgets and zero-value denominators safely (Section 9)
        public double PercentageConsumed => BudgetAmount > 0 
            ? (double)Math.Round((ActualSpending / BudgetAmount) * 100, 1) 
            : (ActualSpending > 0 ? 100.0 : 0.0);

        public string ProgressBarClass
        {
            get
            {
                if (BudgetAmount <= 0) return "bg-secondary";
                if (IsOverBudget) return "bg-danger";
                if (PercentageConsumed >= 80) return "bg-warning";
                return "bg-success";
            }
        }
    }

    public class MonthlyBudgetProgressItem
    {
        public int Id { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal BudgetAmount { get; set; }
        public decimal ActualSpending { get; set; }

        public decimal RemainingBudget => Math.Round(BudgetAmount - ActualSpending, 2, MidpointRounding.AwayFromZero);
        public bool IsOverBudget => ActualSpending > BudgetAmount;
        public decimal OverBudgetAmount => IsOverBudget ? Math.Round(ActualSpending - BudgetAmount, 2, MidpointRounding.AwayFromZero) : 0m;

        public double PercentageConsumed => BudgetAmount > 0 
            ? (double)Math.Round((ActualSpending / BudgetAmount) * 100, 1) 
            : (ActualSpending > 0 ? 100.0 : 0.0);

        public string ProgressBarClass
        {
            get
            {
                if (BudgetAmount <= 0) return "bg-secondary";
                if (IsOverBudget) return "bg-danger";
                if (PercentageConsumed >= 80) return "bg-warning";
                return "bg-success";
            }
        }
    }

    public class BudgetIndexViewModel
    {
        public int SelectedYear { get; set; } = DateTime.Today.Year;
        public int SelectedMonth { get; set; } = DateTime.Today.Month;

        public List<MonthlyBudgetProgressItem> MonthlyBudgets { get; set; } = new();
        public List<CategoryBudgetProgressViewModel> CategoryBudgets { get; set; } = new();

        public decimal TotalAnnualBudget => MonthlyBudgets.Sum(b => b.BudgetAmount);
        public decimal TotalAnnualSpending => MonthlyBudgets.Sum(b => b.ActualSpending);
    }
}

