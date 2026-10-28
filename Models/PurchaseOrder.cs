using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace PersonalExpenseTracker.Models
{
    public class PurchaseOrder
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Platform is required")]
        [Display(Name = "Platform")]
        public int PlatformId { get; set; }

        [ForeignKey(nameof(PlatformId))]
        public Platform? Platform { get; set; }

        [StringLength(100, ErrorMessage = "External order number cannot exceed 100 characters")]
        [Display(Name = "External Order / Receipt #")]
        public string? ExternalOrderNumber { get; set; }

        [Required(ErrorMessage = "Order date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Order Date")]
        public DateTime OrderDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Order status is required")]
        [RegularExpression("^(Completed|Pending|Cancelled)$", ErrorMessage = "Status must be 'Completed', 'Pending', or 'Cancelled'")]
        [Display(Name = "Order Status")]
        public string OrderStatus { get; set; } = Models.OrderStatus.Completed;

        [StringLength(50, ErrorMessage = "Payment method cannot exceed 50 characters")]
        [Display(Name = "Payment Method")]
        public string PaymentMethod { get; set; } = "UPI (GPay / PhonePe / Paytm)";

        [Range(0, 9999999999.99, ErrorMessage = "Items subtotal cannot be negative")]
        [Column(TypeName = "decimal(12,2)")]
        [Display(Name = "Item Subtotal (₹)")]
        public decimal ItemSubtotal { get; set; }

        [Range(0, 999999.99, ErrorMessage = "Delivery fee cannot be negative")]
        [Column(TypeName = "decimal(12,2)")]
        [Display(Name = "Delivery Fee (₹)")]
        public decimal DeliveryFee { get; set; }

        [Range(0, 999999.99, ErrorMessage = "Packaging fee cannot be negative")]
        [Column(TypeName = "decimal(12,2)")]
        [Display(Name = "Packaging Fee (₹)")]
        public decimal PackagingFee { get; set; }

        [Range(0, 999999.99, ErrorMessage = "Platform fee cannot be negative")]
        [Column(TypeName = "decimal(12,2)")]
        [Display(Name = "Platform Fee (₹)")]
        public decimal PlatformFee { get; set; }

        [Range(0, 999999.99, ErrorMessage = "Taxes cannot be negative")]
        [Column(TypeName = "decimal(12,2)")]
        [Display(Name = "Taxes & GST (₹)")]
        public decimal Taxes { get; set; }

        [Range(0, 999999.99, ErrorMessage = "Other charges cannot be negative")]
        [Column(TypeName = "decimal(12,2)")]
        [Display(Name = "Other Charges (₹)")]
        public decimal OtherCharges { get; set; }

        [Range(0, 9999999999.99, ErrorMessage = "Discount cannot be negative")]
        [Column(TypeName = "decimal(12,2)")]
        [Display(Name = "Discount / Coupon (₹)")]
        public decimal Discount { get; set; }

        [Required(ErrorMessage = "Final bill amount is required")]
        [Range(0.00, 9999999999.99, ErrorMessage = "Final bill amount cannot be negative")]
        [Column(TypeName = "decimal(12,2)")]
        [Display(Name = "Final Bill Amount (₹)")]
        public decimal FinalBillAmount { get; set; }

        [Range(0, 9999999999.99, ErrorMessage = "Refund amount cannot be negative")]
        [Column(TypeName = "decimal(12,2)")]
        [Display(Name = "Refund / Return Amount (₹)")]
        public decimal RefundAmount { get; set; }

        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        [Display(Name = "Notes")]
        public string? Notes { get; set; }

        [Required(ErrorMessage = "Source type is required")]
        [StringLength(50, ErrorMessage = "Source type cannot exceed 50 characters")]
        [Display(Name = "Source Type")]
        public string SourceType { get; set; } = SourceTypes.Manual;

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Updated At")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation collection for 1-to-many Order -> Items
        public List<OrderItem> Items { get; set; } = new();

        // -------------------------------------------------------------
        // BACKWARDS COMPATIBILITY PROPERTIES & ALIASES
        // -------------------------------------------------------------
        private string? _legacyPlatformName;

        [NotMapped]
        public string OrderNumber
        {
            get => ExternalOrderNumber ?? (Id > 0 ? $"ORD-{Id:D4}" : string.Empty);
            set => ExternalOrderNumber = value;
        }

        [NotMapped]
        public string Status
        {
            get => OrderStatus;
            set => OrderStatus = value;
        }

        [NotMapped]
        public decimal SubtotalAmount
        {
            get => ItemSubtotal;
            set => ItemSubtotal = value;
        }

        [NotMapped]
        public decimal DiscountAmount
        {
            get => Discount;
            set => Discount = value;
        }

        [NotMapped]
        public decimal TotalAmount
        {
            get => FinalBillAmount;
            set => FinalBillAmount = value;
        }

        [NotMapped]
        public string PlatformName
        {
            get => Platform?.Name ?? _legacyPlatformName ?? "Direct / In-Store";
            set => _legacyPlatformName = value;
        }

        /// <summary>
        /// Net purchase spending = completed purchase bills minus recorded refunds.
        /// Cancelled or Pending orders contribute 0.00 to current realized spending.
        /// </summary>
        [NotMapped]
        public decimal NetPurchaseSpending
        {
            get
            {
                if (!string.Equals(OrderStatus, Models.OrderStatus.Completed, StringComparison.OrdinalIgnoreCase)) 
                    return 0m;
                return Math.Max(0m, FinalBillAmount - RefundAmount);
            }
        }

        /// <summary>
        /// Strictly recalculates all totals on the server before database persistence.
        /// </summary>
        public void RecalculateTotals()
        {
            if (Items != null && Items.Any())
            {
                ItemSubtotal = Math.Round(Items.Sum(i => i.LineSubtotal), 2, MidpointRounding.AwayFromZero);
            }

            decimal preDiscountTotal = ItemSubtotal + DeliveryFee + PackagingFee + PlatformFee + Taxes + OtherCharges;
            FinalBillAmount = Math.Max(0m, Math.Round(preDiscountTotal - Discount, 2, MidpointRounding.AwayFromZero));
            
            // Refund cannot exceed final bill
            if (RefundAmount > FinalBillAmount)
            {
                RefundAmount = FinalBillAmount;
            }
        }
    }
}
