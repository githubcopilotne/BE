using BE.DTOs;
using BE.Models;

namespace BE.Services.Interfaces
{
    public interface IGhnService
    {
        Task<ApiResponse<object>> GetProvinces();
        Task<ApiResponse<object>> GetDistricts(int provinceId);
        Task<ApiResponse<object>> GetWards(int districtId);
        Task<ApiResponse<object>> CalculateShippingFee(int districtId, string wardCode, int weight, int insuranceValue);
        Task<ApiResponse<object>> CreateShippingOrder(Order order);
    }
}
