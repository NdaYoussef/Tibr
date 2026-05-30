using Tibr.Domain.Entities;

namespace Tibr.Application.InfrastructureContracts
{
    public interface IOrderQueryService
    {
        Task<Order?> GetByIdWithDetailsAsync(long id);
        Task<IEnumerable<Order>> GetAllWithDetailsAsync();
        Task<IEnumerable<Order>> GetByUserIdWithDetailsAsync(long userId);
    }
}
