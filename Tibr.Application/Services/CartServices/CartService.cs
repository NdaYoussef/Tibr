using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MapsterMapper;
using Tibr.Application.Dtos;
using Tibr.Domain.Entities;
using Tibr.Domain.IRepositories;
using Tibr.Domain.ResultPattern;

namespace Tibr.Application.Services.CartServices
{
    public class CartService : ICartService
    {
        private readonly ICartRepository _cartRepository;
        private readonly IProductRepository _productRepository;
        private readonly IMapper _mapper;

        public CartService(
            ICartRepository cartRepository,
            IProductRepository productRepository,
            IMapper mapper)
        {
            _cartRepository = cartRepository;
            _productRepository = productRepository;
            _mapper = mapper;
        }

        public async Task<Result<CartDto>> GetCartByUserIdAsync(long userId)
        {
            var cart = await _cartRepository.GetCartByUserIdAsync(userId);

            if (cart == null)
            {
                return Result<CartDto>.Success(new CartDto
                {
                    UserId = userId,
                    CartItems = []
                });
            }

            return Result<CartDto>.Success(_mapper.Map<CartDto>(cart));
        }

        public async Task<Result<CartDto>> AddToCartAsync(long userId, AddToCartDto dto)
        {
            if (dto.Quantity <= 0)
            {
                return Result<CartDto>.Failure("Quantity must be greater than zero.");
            }

            // 1. Check if product exists in product table first
            var product = await _productRepository.GetByIdAsync(dto.ProductId);

            if (product == null || product.IsDeleted)
            {
                return Result<CartDto>.Failure($"Product with ID {dto.ProductId} does not exist.");
            }

            // 2. Get or create cart for the user
            var cart = await _cartRepository.GetCartByUserIdAsync(userId);

            if (cart == null)
            {
                cart = new Cart
                {
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    CartItems = []
                };
                await _cartRepository.AddAsync(cart);
                await _cartRepository.SaveChangesAsync();
            }

            // 3. Check if item already in cart to increase quantity, else add new item
            var existingCartItem = cart.CartItems
                .FirstOrDefault(ci => ci.ProductId == dto.ProductId && !ci.IsDeleted);

            if (existingCartItem != null)
            {
                existingCartItem.Quantity += dto.Quantity;
                existingCartItem.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                var newCartItem = new CartItem
                {
                    ProductId = dto.ProductId,
                    Quantity = dto.Quantity,
                    UnitPrice = product.SellPrice,
                    CreatedAt = DateTime.UtcNow,
                    CartId = cart.Id
                };
                cart.CartItems.Add(newCartItem);
            }

            await _cartRepository.SaveChangesAsync();

            // Return the updated cart DTO
            return await GetCartByUserIdAsync(userId);
        }

        public async Task<Result<CartDto>> RemoveFromCartAsync(long userId, long cartItemId)
        {
            var cart = await _cartRepository.GetCartByUserIdAsync(userId);

            if (cart == null)
            {
                return Result<CartDto>.Failure("Cart not found.");
            }

            var cartItem = cart.CartItems.FirstOrDefault(ci => ci.Id == cartItemId && !ci.IsDeleted);
            if (cartItem == null)
            {
                return Result<CartDto>.Failure($"Cart item with ID {cartItemId} not found in user's cart.");
            }

            _cartRepository.RemoveCartItem(cartItem);
            await _cartRepository.SaveChangesAsync();

            return await GetCartByUserIdAsync(userId);
        }

        public async Task<Result<bool>> ClearCartAsync(long userId)
        {
            var cart = await _cartRepository.GetCartByUserIdAsync(userId);

            if (cart == null)
            {
                return Result<bool>.Success(true);
            }

            var activeItems = cart.CartItems.Where(ci => !ci.IsDeleted).ToList();
            if (activeItems.Any())
            {
                _cartRepository.RemoveCartItemsRange(activeItems);
                await _cartRepository.SaveChangesAsync();
            }

            return Result<bool>.Success(true);
        }

        public async Task<Result<CartDto>> UpdateCartItemQuantityAsync(long userId, long cartItemId, int quantity)
        {
            if (quantity <= 0)
            {
                return Result<CartDto>.Failure("Quantity must be greater than zero.");
            }

            var cart = await _cartRepository.GetCartByUserIdAsync(userId);

            if (cart == null)
            {
                return Result<CartDto>.Failure("Cart not found.");
            }

            var cartItem = cart.CartItems.FirstOrDefault(ci => ci.Id == cartItemId && !ci.IsDeleted);
            if (cartItem == null)
            {
                return Result<CartDto>.Failure($"Cart item with ID {cartItemId} not found in user's cart.");
            }

            cartItem.Quantity = quantity;
            cartItem.UpdatedAt = DateTime.UtcNow;

            await _cartRepository.SaveChangesAsync();

            return await GetCartByUserIdAsync(userId);
        }
    }
}