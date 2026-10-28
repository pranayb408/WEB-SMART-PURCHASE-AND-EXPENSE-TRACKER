using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PersonalExpenseTracker.Models
{
    public class Platform
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Platform name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Platform name must be between 2 and 100 characters")]
        [Display(Name = "Platform Name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Platform type is required")]
        [StringLength(50, ErrorMessage = "Platform type cannot exceed 50 characters")]
        [Display(Name = "Platform Type")]
        public string PlatformType { get; set; } = "Other";

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        // Navigation property for 1-to-many relationship: Platform -> PurchaseOrders
        public List<PurchaseOrder> PurchaseOrders { get; set; } = new();
    }
}

