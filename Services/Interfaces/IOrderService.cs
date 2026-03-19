using BE.DTOs;
using BE.DTOs.Order;
using BE.Models;

namespace BE.Services.Interfaces
{
    public interface IOrderService
    {
        Task<ApiResponse<object>> CreateOrder(int userId, CreateOrderRequest request);
        Task<ApiResponse<object>> VnPayReturn(IQueryCollection queryParams);
        string CreateVnPayUrl(int orderId, decimal totalMoney, string ipAddress);
        Task CancelOrder(Order order);
        Task<ApiResponse<object>> GetOrders(int? status, int? paymentStatus, int? paymentMethod, string? search, int page, int pageSize);
    }
}
