using System.ComponentModel.DataAnnotations;

namespace Tibr.MVC.Models.Categories
{

    public class CreateCategoryViewModel
    {
        [Required(ErrorMessage = "Category name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters")]
        [Display(Name = "Category Name")]
        public string Name { get; set; } = string.Empty;
    }
}
