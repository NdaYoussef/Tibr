using System;
using System.Collections.Generic;
using System.Text;

namespace Tibr.Application.Dtos.FavoriteDtos
{
    public class FavoriteProductResponse
    {
        public long ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; } 
    }
}
