using System;
using System.Collections.Generic;

namespace PersonalExpenseTracker.Models
{
    public class BillCalculationResult
    {
        public bool IsValid => ValidationErrors.Count == 0 && !HasDiscrepancy;

        public List<string> ValidationErrors { get; set; } = new();

        public decimal ItemSubtotal { get; set; }

        public decimal PreDiscountTotal { get; set; }

        public decimal FinalBillAmount { get; set; }

        public decimal NetPurchaseSpending { get; set; }

        public bool HasDiscrepancy { get; set; }

        public decimal DiscrepancyAmount { get; set; }

        public decimal? OriginalBillAmount { get; set; }

        public decimal CalculatedBillAmount => FinalBillAmount;

        public string? DiscrepancyExplanation { get; set; }
    }
}

