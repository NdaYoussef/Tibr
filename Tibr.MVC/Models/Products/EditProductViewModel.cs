using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using Tibr.Domain.Enums;

namespace Tibr.MVC.Models.Products
{

    // EDIT PRODUCT PAGE
    public class EditProductViewModel
    {
        public long Id { get; set; }

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
        [Range(0.001, 9999.999)]
        [Display(Name = "Purity")]
        public decimal Purity { get; set; }

        [Required]
        [Range(0.001, 999999)]
        [Display(Name = "Weight (grams)")]
        public decimal Weight { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        [Display(Name = "Buy Price (EGP)")]
        public decimal BuyPrice { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        [Display(Name = "Sell Price (EGP)")]
        public decimal SellPrice { get; set; }

        [Required]
        [Display(Name = "Status")]
        public ProductStatus Status { get; set; }

        [Required]
        [Range(0, long.MaxValue)]
        [Display(Name = "Stock")]
        public long Stock { get; set; }

        [Display(Name = "Product Image")]
        public IFormFile? ImageFile { get; set; }

        public string? ExistingImageUrl { get; set; }

        // Dropdowns
        public IEnumerable<SelectListItem> CategoryOptions { get; set; } = [];
    }
}
