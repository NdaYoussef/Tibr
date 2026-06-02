using System;
using System.Collections.Generic;
using System.Text;
using Tibr.Application.Dtos.FavoriteDtos;
using Tibr.Domain.Entities;
using Tibr.Domain.IRepositories;

namespace Tibr.Application.Services.FavoriteServices
{
    public class FavoriteService : IFavoriteService
    {
        private readonly IFavoriteRepository _favoriteRepository;

        public FavoriteService(IFavoriteRepository favoriteRepository)
        {
            _favoriteRepository = favoriteRepository;
        }

        public async Task<Result<string>> ToggleFavoriteAsync(long userId, long productId)
        {
           
            var existingFavorite = await _favoriteRepository.GetByUniqueKeysAsync(userId, productId);

            if (existingFavorite != null)
            {
               
                await _favoriteRepository.DeleteAsync(existingFavorite);
                var removeResult = await _favoriteRepository.SaveChangesAsync();

                if (removeResult <= 0)
                    return Result<string>.Failure("Failed to remove from favorites.");

                return Result<string>.Success("Removed from favorites successfully.");
            }

            var favorite = new Favorite
            {
                UserId = userId,
                ProductId = productId
            };

            await _favoriteRepository.AddAsync(favorite);
            var addResult = await _favoriteRepository.SaveChangesAsync();

            if (addResult <= 0)
                return Result<string>.Failure("Failed to add to favorites.");

            return Result<string>.Success("Added to favorites successfully.");
        }

        public async Task<Result<IEnumerable<FavoriteProductResponse>>> GetUserFavoritesAsync(long userId)
        {
            var favorites = await _favoriteRepository.GetUserFavoritesWithProductsAsync(userId);

            var response = favorites.Select(f => new FavoriteProductResponse
            {
                ProductId = f.ProductId,
                ProductName = f.Product?.Name ?? "Unknown Product",
                Price = f.Product?.BuyPrice ?? 0,                    
                ImageUrl = f.Product?.ImageUrl
            });

            return Result<IEnumerable<FavoriteProductResponse>>.Success(response);
        }
    }
}
