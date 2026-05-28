using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Tibr.Application.Dtos;
using Tibr.Domain.Entities;
using Tibr.Domain.ResultPattern;
using Tibr.Infrastructure.Contexts;

namespace Tibr.Application.Services
{
    public class CartService : ICartService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public CartService(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<Result<CartDto>> GetCartByUserIdAsync(long userId)
        {
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId && !c.IsDeleted);

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
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == dto.ProductId && !p.IsDeleted);

            if (product == null)
            {
                return Result<CartDto>.Failure($"Product with ID {dto.ProductId} does not exist.");
            }

            // 2. Get or create cart for the user
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId && !c.IsDeleted);

            if (cart == null)
            {
                cart = new Cart
                {
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    CartItems = []
                };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
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

            await _context.SaveChangesAsync();

            // Return the updated cart DTO
            return await GetCartByUserIdAsync(userId);
        }

        public async Task<Result<CartDto>> RemoveFromCartAsync(long userId, long cartItemId)
        {
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId && !c.IsDeleted);

            if (cart == null)
            {
                return Result<CartDto>.Failure("Cart not found.");
            }

            var cartItem = cart.CartItems.FirstOrDefault(ci => ci.Id == cartItemId && !ci.IsDeleted);
            if (cartItem == null)
            {
                return Result<CartDto>.Failure($"Cart item with ID {cartItemId} not found in user's cart.");
            }

            _context.CartItems.Remove(cartItem);
            await _context.SaveChangesAsync();

            return await GetCartByUserIdAsync(userId);
        }

        public async Task<Result<bool>> ClearCartAsync(long userId)
        {
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId && !c.IsDeleted);

            if (cart == null)
            {
                return Result<bool>.Success(true);
            }

            var activeItems = cart.CartItems.Where(ci => !ci.IsDeleted).ToList();
            if (activeItems.Any())
            {
                _context.CartItems.RemoveRange(activeItems);
                await _context.SaveChangesAsync();
            }

            return Result<bool>.Success(true);
        }
    }
}
