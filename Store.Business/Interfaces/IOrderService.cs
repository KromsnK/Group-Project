using Store.Business.DTO;

namespace Store.Business.Interfaces;

public interface IOrderService
{
    Task<OrderDto> CreateOrderAsync(int userId);
    Task<IEnumerable<OrderDto>> GetUserOrdersAsync(int userId);
    Task<OrderDto> UpdateOrderStatusAsync(int orderId, string newStatus);
    Task<OrderDto?> GetOrderByIdAsync(int id, int userId);
}
