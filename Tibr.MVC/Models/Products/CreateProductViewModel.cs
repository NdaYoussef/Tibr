using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using Tibr.Domain.Enums;

namespace Tibr.MVC.Models.Products
{
    // ADD PRODUCT PAGE
    public class CreateProductViewModel
    {
        [Required(ErrorMessage = "Product name is required")]
        [StringLength(200, MinimumLength = 2)]
        [Display(Name = "Product Name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Category is required")]
        [Display(Name = "Category")]
        public long CategoryId { get; set; }

        [Required(ErrorMessage = "Metal type is required")]
        [Display(Name = "Metal Type")]
        public MetalType MetalType { get; set; }

        [Required]
        [Range(0.001, 9999.999, ErrorMessage = "Purity must be between 0.001 and 9999.999")]
        [Display(Name = "Purity")]
        public decimal Purity { get; set; }

        [Required]
        [Range(0.001, 999999, ErrorMessage = "Weight must be positive")]
        [Display(Name = "Weight (grams)")]
        public decimal Weight { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Buy price must be positive")]
        [Display(Name = "Buy Price (EGP)")]
        public decimal BuyPrice { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Sell price must be positive")]
        [Display(Name = "Sell Price (EGP)")]
        public decimal SellPrice { get; set; }

        [Required]
        [Range(0, long.MaxValue, ErrorMessage = "Stock cannot be negative")]
        [Display(Name = "Initial Stock")]
        public long Stock { get; set; }

        [Display(Name = "Product Image")]
        public IFormFile? ImageFile { get; set; }

        public string? ExistingImageUrl { get; set; }

        // Dropdowns populated by controller
        public IEnumerable<SelectListItem> CategoryOptions { get; set; } = [];
    }
}
