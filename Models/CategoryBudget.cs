using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PersonalExpenseTracker.Models
{
    public class CategoryBudget
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Year is required")]
        [Range(2000, 2100, ErrorMessage = "Year must be between 2000 and 2100")]
        [Display(Name = "Year")]
        public int Year { get; set; }

        [Required(ErrorMessage = "Month is required")]
        [Range(1, 12, ErrorMessage = "Month must be between 1 and 12")]
        [Display(Name = "Month")]
        public int Month { get; set; }

        [Required(ErrorMessage = "Category is required")]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        [ForeignKey(nameof(CategoryId))]
        public Category? Category { get; set; }

        [Required(ErrorMessage = "Budget amount is required")]
        [Range(0, 9999999999.99, ErrorMessage = "Budget amount cannot be negative")]
        [Column(TypeName = "decimal(12,2)")]
        [Display(Name = "Allocated Budget (₹)")]
        public decimal Amount { get; set; }
    }
}

