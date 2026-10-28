using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PersonalExpenseTracker.Models
{
    public class OrderItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PurchaseOrderId { get; set; }

        [ForeignKey(nameof(PurchaseOrderId))]
        public PurchaseOrder? PurchaseOrder { get; set; }

        [Required(ErrorMessage = "Category is required")]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        [ForeignKey(nameof(CategoryId))]
        public Category? Category { get; set; }

        [Required(ErrorMessage = "Item name is required")]
        [StringLength(150, MinimumLength = 1, ErrorMessage = "Item name must not exceed 150 characters")]
        [Display(Name = "Item / Product Name")]
        public string ItemName { get; set; } = string.Empty;

        [Range(1, 10000, ErrorMessage = "Quantity must be between 1 and 10,000")]
        [Display(Name = "Quantity")]
        public int Quantity { get; set; } = 1;

        [Required(ErrorMessage = "Unit price is required")]
        [Range(0.00, 10000000.00, ErrorMessage = "Unit price must be between 0.00 and 10,000,000.00")]
        [Column(TypeName = "decimal(12,2)")]
        [Display(Name = "Unit Price (₹)")]
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// Server-side computed LineSubtotal: Quantity × UnitPrice rounded to 2 decimal places.
        /// </summary>
        [NotMapped]
        [Display(Name = "Line Subtotal (₹)")]
        public decimal LineSubtotal => Math.Round(Quantity * UnitPrice, 2, MidpointRounding.AwayFromZero);

        /// <summary>
        /// TotalPrice for backwards compatibility with existing views and tests.
        /// </summary>
        [Column(TypeName = "decimal(12,2)")]
        [Display(Name = "Total Price (₹)")]
        public decimal TotalPrice
        {
            get => LineSubtotal;
            set { /* Recalculated dynamically from Quantity * UnitPrice */ }
        }

        [NotMapped]
        public string CategoryName => Category?.Name ?? string.Empty;

        public void RecalculateTotal()
        {
            // Dynamically computed via LineSubtotal
        }
    }
}

