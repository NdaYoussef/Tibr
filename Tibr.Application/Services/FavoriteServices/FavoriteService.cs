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
        public async Task<bool> IsFavoriteAsync(long userId, long productId)
        {
            Console.WriteLine($"Checking if product {productId} is a favorite for user {userId}.");
            var favorite = await _favoriteRepository.GetByUniqueKeysAsync(userId, productId);

            var isFavorite = favorite?.IsDeleted == false;

            return isFavorite;
        }
        public async Task<Result<string>> ToggleFavoriteAsync(long userId, long productId)
        {
           
            var existingFavorite = await _favoriteRepository.GetByUniqueKeysAsync(userId, productId);

            if (existingFavorite != null)
            {
                if (existingFavorite.IsDeleted == false)
                {
                    await _favoriteRepository.DeleteAsync(existingFavorite);
                    var removeResult = await _favoriteRepository.SaveChangesAsync();

                    if (removeResult <= 0)
                        return Result<string>.Failure("Failed to remove from favorites.");

                    return Result<string>.Success("Removed from favorites successfully.");
                }
                else
                {
                    existingFavorite.IsDeleted = false;
                    await _favoriteRepository.UpdateIsDeleteAsync(existingFavorite);
                    var restoreResult = await _favoriteRepository.SaveChangesAsync();
                    if (restoreResult <= 0)
                        return Result<string>.Failure("Failed to restore to favorites.");
                    return Result<string>.Success("Restored to favorites successfully.");
                }
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

        public async Task<IEnumerable<FavoriteProductResponse>> GetUserFavoritesAsync(long userId)
        {
            var favorites = await _favoriteRepository.GetUserFavoritesWithProductsAsync(userId);

            var response = favorites.Where(f => f.UserId == userId && !f.IsDeleted).Select(f => new FavoriteProductResponse
            {
                ProductId = f.ProductId,
                ProductName = f.Product?.Name ?? "Unknown Product",
                Price = f.Product?.BuyPrice ?? 0,
                ImageUrl = f.Product?.ImageUrl ?? "Unknown Image"
            });

            return response;
        }
    }
}
