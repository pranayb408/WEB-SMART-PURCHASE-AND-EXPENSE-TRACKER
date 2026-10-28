using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PersonalExpenseTracker.Models
{
    public class Transaction
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Title must be between 2 and 100 characters")]
        [Display(Name = "Transaction Title")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Type is required")]
        [RegularExpression("^(Income|Expense)$", ErrorMessage = "Transaction type must be either 'Income' or 'Expense'")]
        [Display(Name = "Type")]
        public string Type { get; set; } = "Expense";

        [Required(ErrorMessage = "Category is required")]
        [StringLength(50, ErrorMessage = "Category cannot exceed 50 characters")]
        [Display(Name = "Category")]
        public string Category { get; set; } = string.Empty;

        [Required(ErrorMessage = "Amount is required")]
        [Range(0.01, 9999999999.99, ErrorMessage = "Amount must be greater than zero")]
        [Column(TypeName = "decimal(12,2)")]
        [Display(Name = "Amount (₹)")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Transaction date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Date")]
        public DateTime Date { get; set; } = DateTime.Today;

        [StringLength(300, ErrorMessage = "Notes cannot exceed 300 characters")]
        [Display(Name = "Notes")]
        public string? Notes { get; set; }

        [StringLength(50, ErrorMessage = "Platform cannot exceed 50 characters")]
        [Display(Name = "Purchase Platform / App")]
        public string? Platform { get; set; } = "Direct / In-Store";

        [StringLength(60, ErrorMessage = "Order or Receipt ID cannot exceed 60 characters")]
        [Display(Name = "Order / Receipt #")]
        public string? OrderNumber { get; set; }

        [StringLength(50, ErrorMessage = "Payment method cannot exceed 50 characters")]
        [Display(Name = "Payment Method")]
        public string? PaymentMethod { get; set; }
    }
}
