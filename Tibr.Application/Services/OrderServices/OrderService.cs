using Mapster;
using Tibr.Application.Dtos;
using Tibr.Application.InfrastructureContracts;
using Tibr.Domain.Entities;
using Tibr.Domain.IRepositories;
using Tibr.Domain.ResultPattern;

namespace Tibr.Application.Services.OrderServices
{
    public class OrderService : IOrderService
    {
        private readonly IGenericRepository<Order, long> _orderRepository;
        private readonly IGenericRepository<OrderItem, long> _orderItemRepository;
        private readonly IGenericRepository<Product, long> _productRepository;
        private readonly IOrderQueryService _orderQueryService;

        public OrderService(
            IGenericRepository<Order, long> orderRepository,
            IGenericRepository<OrderItem, long> orderItemRepository,
            IGenericRepository<Product, long> productRepository,
            IOrderQueryService orderQueryService
        )
        {
            _orderRepository = orderRepository;
            _orderItemRepository = orderItemRepository;
            _productRepository = productRepository;
            _orderQueryService = orderQueryService;
        }

        public async Task<Result<OrderDto>> GetByIdAsync(long id)
        {
            var order = await _orderQueryService.GetByIdWithDetailsAsync(id);

            if (order is null)
                return Result<OrderDto>.Failure($"Order with ID {id} not found.");

            return Result<OrderDto>.Success(order.Adapt<OrderDto>());
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
