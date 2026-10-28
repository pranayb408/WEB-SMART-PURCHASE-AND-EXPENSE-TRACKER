using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PersonalExpenseTracker.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Category name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Category name must be between 2 and 100 characters")]
        [Display(Name = "Category Name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Category type is required")]
        [StringLength(50, ErrorMessage = "Category type cannot exceed 50 characters")]
        [Display(Name = "Category Type")]
        public string CategoryType { get; set; } = "Expense";

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public List<OrderItem> OrderItems { get; set; } = new();
        public List<CategoryBudget> CategoryBudgets { get; set; } = new();
    }
}

