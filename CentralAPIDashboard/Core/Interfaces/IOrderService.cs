using Core.Models.DTO;

namespace Core.Interfaces
{
    public interface IOrderService
    {
        Task<OrderResponseDto> CreateOrderAsync(string userId, CreateOrderDto orderDto);
        Task<List<OrderResponseDto>> GetUserOrdersAsync(string userId);
        Task<OrderResponseDto?> GetOrderByIdAsync(string orderId, string userId);
    }
}
