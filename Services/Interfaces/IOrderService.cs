using BE.DTOs;
using BE.DTOs.Order;

namespace BE.Services.Interfaces
{
    public interface IOrderService
    {
        Task<ApiResponse<object>> CreateOrder(int userId, CreateOrderRequest request);
    }
}
