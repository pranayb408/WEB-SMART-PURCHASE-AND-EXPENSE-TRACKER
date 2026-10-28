using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using PersonalExpenseTracker.Models;

namespace PersonalExpenseTracker.ViewModels
{
    public class PurchaseOrderFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Platform is required")]
        [Display(Name = "Platform / Store")]
        public int PlatformId { get; set; }

        [StringLength(100, ErrorMessage = "External order number cannot exceed 100 characters")]
        [Display(Name = "External Order / Receipt #")]
        public string? ExternalOrderNumber { get; set; }

        [Required(ErrorMessage = "Order date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Order Date")]
        public DateTime OrderDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Order status is required")]
        [Display(Name = "Order Status")]
        public string OrderStatus { get; set; } = Models.OrderStatus.Completed;

        [StringLength(50, ErrorMessage = "Payment method cannot exceed 50 characters")]
        [Display(Name = "Payment Method")]
        public string PaymentMethod { get; set; } = "UPI (GPay / PhonePe / Paytm)";

        [Range(0, 999999.99, ErrorMessage = "Delivery fee cannot be negative")]
        [Display(Name = "Delivery Fee (₹)")]
        public decimal DeliveryFee { get; set; } = 0.00m;

        [Range(0, 999999.99, ErrorMessage = "Packaging fee cannot be negative")]
        [Display(Name = "Packaging Fee (₹)")]
        public decimal PackagingFee { get; set; } = 0.00m;

        [Range(0, 999999.99, ErrorMessage = "Platform fee cannot be negative")]
        [Display(Name = "Platform Fee (₹)")]
        public decimal PlatformFee { get; set; } = 0.00m;

        [Range(0, 999999.99, ErrorMessage = "Taxes cannot be negative")]
        [Display(Name = "Taxes & GST (₹)")]
        public decimal Taxes { get; set; } = 0.00m;

        [Range(0, 999999.99, ErrorMessage = "Other charges cannot be negative")]
        [Display(Name = "Other Charges (₹)")]
        public decimal OtherCharges { get; set; } = 0.00m;

        [Range(0, 9999999999.99, ErrorMessage = "Discount cannot be negative")]
        [Display(Name = "Discount / Coupon (₹)")]
        public decimal Discount { get; set; } = 0.00m;

        [Range(0, 9999999999.99, ErrorMessage = "Refund amount cannot be negative")]
        [Display(Name = "Refund / Return (₹)")]
        public decimal RefundAmount { get; set; } = 0.00m;

        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        [Display(Name = "Notes")]
        public string? Notes { get; set; }

        [StringLength(50, ErrorMessage = "Source type cannot exceed 50 characters")]
        [Display(Name = "Source Type")]
        public string SourceType { get; set; } = SourceTypes.Manual;

        // Dynamic items collection
        public List<PurchaseOrderItemInputModel> Items { get; set; } = new();

        // Select lists populated by controller
        public List<SelectListItem> PlatformOptions { get; set; } = new();
        public List<SelectListItem> CategoryOptions { get; set; } = new();
        public List<SelectListItem> StatusOptions { get; set; } = new();
        public List<SelectListItem> PaymentMethodOptions { get; set; } = new();
        public List<SelectListItem> SourceTypeOptions { get; set; } = new();

        // Computed summary values for rendering
        public decimal ItemSubtotal { get; set; }
        public decimal FinalBillAmount { get; set; }
        public decimal NetPurchaseSpending { get; set; }
    }

    public class PurchaseOrderItemInputModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Item name is required")]
        [StringLength(150, MinimumLength = 1, ErrorMessage = "Item name must be between 1 and 150 characters")]
        [Display(Name = "Item / Product Name")]
        public string ItemName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Category is required")]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        [Range(1, 10000, ErrorMessage = "Quantity must be between 1 and 10,000")]
        [Display(Name = "Qty")]
        public int Quantity { get; set; } = 1;

        [Required(ErrorMessage = "Unit price is required")]
        [Range(0.00, 10000000.00, ErrorMessage = "Unit price must be between 0.00 and 10,000,000.00")]
        [Display(Name = "Unit Price (₹)")]
        public decimal UnitPrice { get; set; }

        [Display(Name = "Line Subtotal (₹)")]
        public decimal LineSubtotal => Math.Round(Quantity * UnitPrice, 2, MidpointRounding.AwayFromZero);
    }
}

