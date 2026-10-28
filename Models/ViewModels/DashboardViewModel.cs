using System.Collections.Generic;

namespace PersonalExpenseTracker.Models.ViewModels
{
    public class DashboardViewModel
    {
        public decimal TotalIncome { get; set; }
        public decimal TotalExpense { get; set; }
        public decimal NetBalance => TotalIncome - TotalExpense;

        public int SelectedMonth { get; set; }
        public int SelectedYear { get; set; }

        public List<Transaction> RecentTransactions { get; set; } = new List<Transaction>();
        public List<BudgetProgressViewModel> BudgetProgressList { get; set; } = new List<BudgetProgressViewModel>();

        // Chart Data Properties
        public List<string> ExpenseCategoryLabels { get; set; } = new List<string>();
        public List<decimal> ExpenseCategoryAmounts { get; set; } = new List<decimal>();
        public List<string> ExpenseCategoryColors { get; set; } = new List<string>();

        // Monthly Trend
        public List<string> MonthLabels { get; set; } = new List<string>();
        public List<decimal> MonthlyIncomes { get; set; } = new List<decimal>();
        public List<decimal> MonthlyExpenses { get; set; } = new List<decimal>();
    }
}

