namespace PersonalExpenseTracker.Models.ViewModels
{
    public class BudgetProgressViewModel
    {
        public int BudgetId { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string CategoryIcon { get; set; } = "bi-tag";
        public string CategoryColor { get; set; } = "#0d6efd";
        public decimal BudgetLimit { get; set; }
        public decimal ActualSpent { get; set; }
        public double Percentage => BudgetLimit > 0 ? (double)Math.Min(100, Math.Round((ActualSpent / BudgetLimit) * 100, 1)) : 0;
        public double RawPercentage => BudgetLimit > 0 ? (double)Math.Round((ActualSpent / BudgetLimit) * 100, 1) : 0;
        public decimal Remaining => BudgetLimit - ActualSpent;
        public bool IsOverBudget => ActualSpent > BudgetLimit;

        public string ProgressBarColor
        {
            get
            {
                if (RawPercentage >= 100) return "bg-danger";
                if (RawPercentage >= 80) return "bg-warning";
                return "bg-success";
            }
        }

        public string StatusBadge
        {
            get
            {
                if (RawPercentage >= 100) return "badge bg-danger";
                if (RawPercentage >= 80) return "badge bg-warning text-dark";
                return "badge bg-success";
            }
        }
    }
}

