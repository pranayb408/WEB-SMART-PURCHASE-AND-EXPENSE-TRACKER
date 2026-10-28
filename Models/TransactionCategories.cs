using System.Collections.Generic;

namespace PersonalExpenseTracker.Models
{
    public static class TransactionCategories
    {
        public static readonly List<string> ExpenseCategories = new()
        {
            "Food & Dining",
            "Groceries",
            "Rent & Housing",
            "Utilities & Bills",
            "Transportation",
            "Entertainment",
            "Healthcare",
            "Education",
            "Shopping",
            "Other Expense"
        };

        public static readonly List<string> IncomeCategories = new()
        {
            "Salary",
            "Freelance",
            "Investment",
            "Allowance / Scholarship",
            "Gifts",
            "Other Income"
        };

        public static List<string> GetAll()
        {
            var list = new List<string>(ExpenseCategories);
            list.AddRange(IncomeCategories);
            return list;
        }

        public static bool IsValid(string category, string type)
        {
            if (string.IsNullOrWhiteSpace(category)) return false;
            if (type == "Income") return IncomeCategories.Contains(category);
            if (type == "Expense") return ExpenseCategories.Contains(category);
            return GetAll().Contains(category);
        }
    }
}

