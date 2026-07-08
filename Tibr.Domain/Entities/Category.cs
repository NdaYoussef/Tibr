using System.Collections.Generic;
using Tibr.Domain.Common.Classes;

namespace Tibr.Domain.Entities
{
    public class Category : BaseEntity<long>
    {
        public string Name { get; set; } = string.Empty;
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;

        public virtual ICollection<Product> Products { get; set; } = [];
    }
}
