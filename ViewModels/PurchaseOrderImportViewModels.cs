using System;
using System.Collections.Generic;
using System.Linq;

namespace PersonalExpenseTracker.ViewModels
{
    public class ImportItemPreview
    {
        public int RowNumber { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int? CategoryId { get; set; }
        public int Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; }
        public decimal LineSubtotal => Quantity * UnitPrice;
        public bool IsValid { get; set; } = true;
        public List<string> Errors { get; set; } = new();
    }

    public class ImportOrderGroupPreview
    {
        public string GroupKey { get; set; } = string.Empty;
        public string ExternalOrderNumber { get; set; } = string.Empty;
        public string PlatformName { get; set; } = string.Empty;
        public int? PlatformId { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.Today;
        public string OrderStatus { get; set; } = Models.OrderStatus.Completed;
        public string PaymentMethod { get; set; } = "UPI";
        public decimal DeliveryFee { get; set; }
        public decimal PackagingFee { get; set; }
        public decimal PlatformFee { get; set; }
        public decimal Taxes { get; set; }
        public decimal OtherCharges { get; set; }
        public decimal Discount { get; set; }
        public decimal RefundAmount { get; set; }

        public List<ImportItemPreview> Items { get; set; } = new();

        public decimal ItemsSubtotal => Items.Sum(i => i.LineSubtotal);
        public decimal PreDiscountTotal => ItemsSubtotal + DeliveryFee + PackagingFee + PlatformFee + Taxes + OtherCharges;
        public decimal FinalBillAmount => Math.Max(0m, Math.Round(PreDiscountTotal - Discount, 2, MidpointRounding.AwayFromZero));
        public decimal NetSpending => string.Equals(OrderStatus, Models.OrderStatus.Completed, StringComparison.OrdinalIgnoreCase) 
            ? Math.Max(0m, FinalBillAmount - RefundAmount) 
            : 0m;

        public bool IsValid { get; set; } = true;
        public bool IsDuplicate { get; set; }
        public List<string> ValidationErrors { get; set; } = new();
    }

    public class ImportPreviewViewModel
    {
        public int TotalRowsRead { get; set; }
        public int TotalGroupsFound => Groups.Count;
        public int ValidGroupsCount => Groups.Count(g => g.IsValid && !g.IsDuplicate);
        public int DuplicateGroupsCount => Groups.Count(g => g.IsDuplicate);
        public int InvalidGroupsCount => Groups.Count(g => !g.IsValid && !g.IsDuplicate);

        public List<ImportOrderGroupPreview> Groups { get; set; } = new();
        public List<string> FileErrors { get; set; } = new();
    }
}

