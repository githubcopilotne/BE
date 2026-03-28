using BE.DTOs;

namespace BE.Services.Interfaces
{
    public interface IGhnService
    {
        Task<ApiResponse<object>> GetProvinces();
        Task<ApiResponse<object>> GetDistricts(int provinceId);
        Task<ApiResponse<object>> GetWards(int districtId);
    }
}
