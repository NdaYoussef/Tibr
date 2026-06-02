using System;
using System.Collections.Generic;
using System.Text;
using Tibr.Application.Dtos.FavoriteDtos;

namespace Tibr.Application.Services.FavoriteServices
{
    public interface IFavoriteService
    {
        Task<Result<string>> ToggleFavoriteAsync(long userId, long productId);
        Task<Result<IEnumerable<FavoriteProductResponse>>> GetUserFavoritesAsync(long userId);
    }
}

