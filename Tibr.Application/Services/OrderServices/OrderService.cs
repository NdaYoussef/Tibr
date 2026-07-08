using Mapster;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using System.Collections;
using Tibr.Application.Dtos;
using Tibr.Application.InfrastructureContracts;
using Tibr.Domain.Entities;
using Tibr.Domain.Enums;
using Tibr.Domain.IRepositories;
using Tibr.Domain.ResultPattern;

namespace Tibr.Application.Services.OrderServices
{
    public class OrderService : IOrderService
    {
        static OrderService()
        {
            TypeAdapterConfig<Order, OrderDto>.NewConfig()
                .Map(dest => dest.PaymentId,
                    src => src.Payments
                        .OrderByDescending(p => p.CreatedAt)
                        .Select(p => p.Id)
                        .Cast<long?>()
                        .FirstOrDefault());
        }
        private readonly IGenericRepository<Order, long> _orderRepository;
        private readonly IGenericRepository<OrderItem, long> _orderItemRepository;
        private readonly IGenericRepository<Product, long> _productRepository;
        private readonly IOrderQueryService _orderQueryService;
        private readonly DbContext _context;

        public OrderService(
            IGenericRepository<Order, long> orderRepository,
            IGenericRepository<OrderItem, long> orderItemRepository,
            IGenericRepository<Product, long> productRepository,
            IOrderQueryService orderQueryService,
            DbContext context
        )
        {
            _orderRepository = orderRepository;
            _orderItemRepository = orderItemRepository;
            _productRepository = productRepository;
            _orderQueryService = orderQueryService;
            _context = context;
        }

        public async Task<Result<OrderDto>> GetByIdAsync(long id)
        {
            var order = await _context.Set<Order>()
                .Where(o => !o.IsDeleted && o.Id == id)
                .Include(o => o.User)
                .Include(o => o.Payments)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync();

            if (order is null)
                return Result<OrderDto>.Failure($"Order with ID {id} not found.");

            var dto = new OrderDto
            {
                Id = order.Id,
                UserId = order.UserId,
                UserFullName = order.User != null
                    ? $"{order.User.FirstName} {order.User.LastName}"
                    : string.Empty,

                OrderNumber = order.OrderNumber,
                TotalAmount = order.TotalAmount,
                PaymentStatus = order.PaymentStatus.ToString(),
                OrderStatus = order.OrderStatus.ToString(),
                CreatedAt = order.CreatedAt,
                PaymentId = order.Payments?
                    .OrderByDescending(p => p.CreatedAt)
                    .FirstOrDefault()?.Id,

                Items = order.OrderItems?.Select(oi => new OrderItemDto
                {
                    ProductId = oi.ProductId,
                    ProductName = oi.Product != null ? oi.Product.Name : string.Empty,
                    Quantity = oi.Quantity,
                    Price = oi.Price
                }).ToList() ?? new List<OrderItemDto>()
            };

            return Result<OrderDto>.Success(dto);
        }

        public async Task<Result<IEnumerable<OrderDto>>> GetAllAsync()
        {
            var orders = await _orderQueryService.GetAllWithDetailsAsync();
            return Result<IEnumerable<OrderDto>>.Success(orders.Adapt<IEnumerable<OrderDto>>());
        }

        public async Task<Result<IEnumerable<OrderDto>>> GetByUserIdAsync(long userId)
        {
            var orders = await _orderQueryService.GetByUserIdWithDetailsAsync(userId);
            return Result<IEnumerable<OrderDto>>.Success(orders.Adapt<IEnumerable<OrderDto>>());
        }

        public async Task<Result<OrderDto>> CreateAsync(CreateOrderDto createDto)
        {
            if (createDto.UserId <= 0)
                return Result<OrderDto>.Failure("UserId is required.");
            if (createDto.Items is null || createDto.Items.Count == 0)
                return Result<OrderDto>.Failure("Order must have at least one item.");

            var order = new Order
            {
                Id = 0,
                UserId = createDto.UserId,
                OrderNumber = $"ORD-{Guid.NewGuid()}",
                OrderStatus = "Pending",
                PaymentStatus = "Unpaid",
                TotalAmount = 0,
                CreatedAt = DateTime.UtcNow,
            };

            await _orderRepository.AddAsync(order);
            await _orderRepository.SaveChangesAsync();

            decimal totalAmount = 0;

            foreach (var itemDto in createDto.Items)
            {
                var product = await _productRepository.GetByIdAsync(itemDto.ProductId);
                if (product is null)
                    return Result<OrderDto>.Failure(
                        $"Product with ID {itemDto.ProductId} not found."
                    );

                var orderItem = new OrderItem
                {
                    Id = 0,
                    OrderId = order.Id,
                    ProductId = itemDto.ProductId,
                    Quantity = itemDto.Quantity,
                    Price = product.SellPrice,
                };

                await _orderItemRepository.AddAsync(orderItem);
                totalAmount += itemDto.Quantity * product.SellPrice;
            }

            order.TotalAmount = totalAmount;
            await _orderRepository.UpdateAsync(order);
            await _orderRepository.SaveChangesAsync();

            var createdOrder = await _orderQueryService.GetByIdWithDetailsAsync(order.Id);

            return Result<OrderDto>.Success(createdOrder.Adapt<OrderDto>()!);
        }

        public async Task<Result<OrderDto>> CreateFromWalletAsync(long userId, WalletCheckoutDto dto)
        {
            if (dto.Items is null || dto.Items.Count == 0)
                return Result<OrderDto>.Failure("Order must have at least one item.");

            var productIds = dto.Items.Select(i => i.ProductId).Distinct().ToList();
            var products = await _context.Set<Product>()
                .Where(p => productIds.Contains(p.Id) && !p.IsDeleted)
                .ToListAsync();

            if (products.Count != productIds.Count)
            {
                var missing = productIds.Except(products.Select(p => p.Id));
                return Result<OrderDto>.Failure($"Products not found: {string.Join(", ", missing)}");
            }

            var productMap = products.ToDictionary(p => p.Id);

            foreach (var item in dto.Items)
            {
                var product = productMap[item.ProductId];
                if (product.Stock < item.Quantity)
                    return Result<OrderDto>.Failure(
                        $"Insufficient stock for '{product.NameEn}'. Available: {product.Stock}, requested: {item.Quantity}.");
            }

            decimal total = dto.Items.Sum(i => i.Quantity * productMap[i.ProductId].SellPrice);

            var wallet = await _context.Set<Wallet>()
                .FirstOrDefaultAsync(w => w.UserId == userId && w.WalletType == WalletType.Cash && !w.IsDeleted);

            if (wallet is null)
                return Result<OrderDto>.Failure("Cash wallet not found.");

            var available = wallet.Balance - wallet.ReservedBalance;
            if (available < total)
                return Result<OrderDto>.Failure(
                    $"Insufficient wallet balance. Available: {available:F2} EGP, required: {total:F2} EGP.");

            await using var transaction = await _context.Database.BeginTransactionAsync();

            wallet.Balance -= total;
            _context.Set<Wallet>().Update(wallet);

            var walletTx = new WalletTransaction
            {
                WalletId = wallet.Id,
                Type = WalletTransactionType.Debit,
                Amount = total,
                ReferenceType = ReferenceType.Order,
                ReferenceId = 0,
                CreatedAt = DateTime.UtcNow
            };
            await _context.Set<WalletTransaction>().AddAsync(walletTx);

            var order = new Order
            {
                UserId = userId,
                OrderNumber = $"ORD-{Guid.NewGuid()}",
                TotalAmount = total,
                PaymentStatus = "Paid",
                OrderStatus = "Processing",
                CreatedAt = DateTime.UtcNow
            };
            await _context.Set<Order>().AddAsync(order);
            await _context.SaveChangesAsync();

            walletTx.ReferenceId = order.Id;
            _context.Set<WalletTransaction>().Update(walletTx);

            foreach (var item in dto.Items)
            {
                var product = productMap[item.ProductId];
                product.Stock -= item.Quantity;
                _context.Set<Product>().Update(product);

                var orderItem = new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    Price = product.SellPrice,
                    CreatedAt = DateTime.UtcNow
                };
                await _context.Set<OrderItem>().AddAsync(orderItem);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var result = await GetByIdAsync(order.Id);
            return result;
        }

        public async Task<Result<OrderDto>> PayWithWalletAsync(long userId, long orderId)
        {
            var order = await _context.Set<Order>()
                .Where(o => o.Id == orderId && o.UserId == userId && !o.IsDeleted)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync();

            if (order is null)
                return Result<OrderDto>.Failure("Order not found.");

            if (order.PaymentStatus == "Paid")
                return Result<OrderDto>.Failure("Order is already paid.");

            decimal total = order.TotalAmount;

            var wallet = await _context.Set<Wallet>()
                .FirstOrDefaultAsync(w => w.UserId == userId && w.WalletType == WalletType.Cash && !w.IsDeleted);

            if (wallet is null)
                return Result<OrderDto>.Failure("Cash wallet not found.");

            var available = wallet.Balance - wallet.ReservedBalance;
            if (available < total)
                return Result<OrderDto>.Failure(
                    $"Insufficient wallet balance. Available: {available:F2} EGP, required: {total:F2} EGP.");

            foreach (var item in order.OrderItems)
            {
                if (item.Product.Stock < item.Quantity)
                    return Result<OrderDto>.Failure(
                        $"Insufficient stock for '{item.Product.NameEn}'. Available: {item.Product.Stock}, requested: {item.Quantity}.");
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            wallet.Balance -= total;
            _context.Set<Wallet>().Update(wallet);

            var walletTx = new WalletTransaction
            {
                WalletId = wallet.Id,
                Type = WalletTransactionType.Debit,
                Amount = total,
                ReferenceType = ReferenceType.Order,
                ReferenceId = order.Id,
                CreatedAt = DateTime.UtcNow
            };
            await _context.Set<WalletTransaction>().AddAsync(walletTx);

            foreach (var item in order.OrderItems)
            {
                item.Product.Stock -= item.Quantity;
                _context.Set<Product>().Update(item.Product);
            }

            order.PaymentStatus = "Paid";
            order.OrderStatus = "Processing";
            order.UpdatedAt = DateTime.UtcNow;
            _context.Set<Order>().Update(order);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var result = await GetByIdAsync(order.Id);
            return result;
        }

        public async Task<Result<OrderDto>> UpdateAsync(long id, UpdateOrderDto updateDto)
        {
            var order = await _orderRepository.GetByIdAsync(id);
            if (order is null)
                return Result<OrderDto>.Failure($"Order with ID {id} not found.");

            if (!string.IsNullOrWhiteSpace(updateDto.PaymentStatus))
                order.PaymentStatus = updateDto.PaymentStatus;

            if (!string.IsNullOrWhiteSpace(updateDto.OrderStatus))
                order.OrderStatus = updateDto.OrderStatus;

            await _orderRepository.UpdateAsync(order);
            await _orderRepository.SaveChangesAsync();

            var updatedOrder = await _orderQueryService.GetByIdWithDetailsAsync(id);

            return Result<OrderDto>.Success(updatedOrder.Adapt<OrderDto>()!);
        }

        public async Task<Result> DeleteAsync(long id)
        {
            var order = await _orderRepository.GetByIdAsync(id);
            if (order is null)
                return Result.Failure($"Order with ID {id} not found.");

            await _orderRepository.DeleteAsync(order);
            await _orderRepository.SaveChangesAsync();
            return Result.Success();
        }
    }
}
