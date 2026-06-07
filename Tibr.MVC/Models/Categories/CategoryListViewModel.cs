using Tibr.MVC.Models.Products;

namespace Tibr.MVC.Models.Categories
{

    // CATEGORY MANAGEMENT PAGE

    public class CategoryListViewModel
    {
        public IEnumerable<CategoryRowViewModel> Categories { get; set; } = [];
        public CreateCategoryViewModel NewCategory { get; set; } = new();
    }

    public class CategoryRowViewModel
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int ProductCount { get; set; }
    }
}
