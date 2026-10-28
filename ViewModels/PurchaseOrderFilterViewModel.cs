using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using PersonalExpenseTracker.Models;

namespace PersonalExpenseTracker.ViewModels
{
    public class PurchaseOrderFilterViewModel
    {
        public int? SelectedYear { get; set; }
        public int? SelectedMonth { get; set; }
        public int? SelectedPlatformId { get; set; }
        public int? SelectedCategoryId { get; set; }
        public string? SelectedStatus { get; set; }
        public string? SearchTerm { get; set; }

        public List<PurchaseOrder> Orders { get; set; } = new();

        // Summary Statistics
        public int TotalOrdersCount { get; set; }
        public int CompletedOrdersCount { get; set; }
        public int PendingOrdersCount { get; set; }
        public int CancelledOrdersCount { get; set; }
        public decimal TotalFinalBills { get; set; }
        public decimal TotalRefunds { get; set; }
        public decimal NetSpending { get; set; }

        // Filter Dropdowns
        public List<SelectListItem> PlatformOptions { get; set; } = new();
        public List<SelectListItem> CategoryOptions { get; set; } = new();
        public List<SelectListItem> StatusOptions { get; set; } = new();
        public List<SelectListItem> MonthOptions { get; set; } = new();
        public List<SelectListItem> YearOptions { get; set; } = new();
    }
}

