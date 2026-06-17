using System.ComponentModel.DataAnnotations;

namespace Tibr.MVC.Models.Categories
{
    public class EditCategoryViewModel
    {
        public long Id { get; set; }

        [Required(ErrorMessage = "Category name is required")]
        [StringLength(100, MinimumLength = 2)]
        [Display(Name = "Category Name")]
        public string Name { get; set; } = string.Empty;
    }
}
