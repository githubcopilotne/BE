using BE.DTOs;
using BE.DTOs.Order;

namespace BE.Services.Interfaces
{
    public interface IOrderService
    {
        Task<ApiResponse<object>> CreateOrder(int userId, CreateOrderRequest request);
        Task<ApiResponse<object>> VnPayReturn(IQueryCollection queryParams);
        string CreateVnPayUrl(int orderId, decimal totalMoney, string ipAddress);
    }
}
