using System;
using PersonalExpenseTracker.Models;

namespace PersonalExpenseTracker.ViewModels
{
    /// <summary>
    /// Type aliases and domain-specific view model components requested in Section 13.
    /// Extends core view models to preserve clean architecture without duplicating fields.
    /// </summary>
    public class OrderCreateViewModel : PurchaseOrderFormViewModel { }

    public class OrderEditViewModel : PurchaseOrderFormViewModel { }

    public class OrderDetailsViewModel
    {
        public PurchaseOrder Order { get; set; } = new();
    }

    public class OrderItemInputViewModel : PurchaseOrderItemInputModel { }

    public class SpendingReportViewModel : ReportsViewModel { }

    public class CategoryBudgetViewModel : CategoryBudgetProgressViewModel { }
}

