using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PersonalExpenseTracker.Models
{
    public class MonthlyBudget
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Year is required")]
        [Range(2000, 2100, ErrorMessage = "Year must be between 2000 and 2100")]
        [Display(Name = "Year")]
        public int Year { get; set; } = DateTime.Today.Year;

        [Required(ErrorMessage = "Month is required")]
        [Range(1, 12, ErrorMessage = "Month must be between 1 and 12")]
        [Display(Name = "Month")]
        public int Month { get; set; } = DateTime.Today.Month;

        [Required(ErrorMessage = "Budget amount is required")]
        [Range(0.01, 9999999999.99, ErrorMessage = "Budget amount must be greater than zero")]
        [Column(TypeName = "decimal(12,2)")]
        [Display(Name = "Budget Limit (₹)")]
        public decimal Amount { get; set; }
    }
}

