using System;
using System.Collections.Generic;

namespace PersonalExpenseTracker.Models.ViewModels
{
    public class TransactionFilterViewModel
    {
        public string? SearchTerm { get; set; }
        public int? CategoryId { get; set; }
        public string? Type { get; set; } // "All", "Income", "Expense"
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public List<Transaction> Transactions { get; set; } = new List<Transaction>();
        public decimal TotalFilteredIncome { get; set; }
        public decimal TotalFilteredExpense { get; set; }
        public decimal FilteredNetBalance => TotalFilteredIncome - TotalFilteredExpense;
    }
}

