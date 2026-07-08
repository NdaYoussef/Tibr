using System;
using System.Collections.Generic;
using System.Text;

namespace Tibr.Application.Dtos.CategoryDto
{
    public class CategoryDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public int ProductCount { get; set; }  
    }
}
