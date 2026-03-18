using BE.DTOs;

namespace BE.Services.Interfaces
{
    public interface IVoucherService
    {
        Task<ApiResponse<object>> ValidateVoucher(string voucherCode);
    }
}
