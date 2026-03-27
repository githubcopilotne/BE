using BE.DTOs;
using BE.DTOs.Voucher;
using BE.Models;
using BE.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BE.Services.Implementations
{
    public class VoucherService : IVoucherService
    {
        private readonly ShopQuanAoContext _context;

        public VoucherService(ShopQuanAoContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<object>> ValidateVoucher(string voucherCode)
        {
            // Trim + uppercase
            var code = voucherCode.Trim().ToUpper();

            if (string.IsNullOrEmpty(code))
                return ApiResponse<object>.ErrorResponse("Vui lòng nhập mã giảm giá");

            // Tìm voucher theo code
            var voucher = await _context.Vouchers
                .FirstOrDefaultAsync(v => v.VoucherCode.ToUpper() == code);

            if (voucher == null)
                return ApiResponse<object>.ErrorResponse("Mã giảm giá không tồn tại");

            // Check status
            if (voucher.Status != 1)
                return ApiResponse<object>.ErrorResponse("Mã giảm giá không hoạt động");

            // Check hết hạn
            if (voucher.ExpiryDate < DateTime.Now)
                return ApiResponse<object>.ErrorResponse("Mã giảm giá đã hết hạn");

            // Check hết lượt
            if (voucher.UsedCount >= voucher.UsageLimit)
                return ApiResponse<object>.ErrorResponse("Mã giảm giá đã hết lượt sử dụng");

            // Hợp lệ → trả về thông tin
            var response = new ValidateVoucherResponse
            {
                VoucherId = voucher.VoucherId,
                VoucherCode = voucher.VoucherCode,
                DiscountType = voucher.DiscountType,
                DiscountValue = voucher.DiscountValue,
                MaxDiscountAmount = voucher.MaxDiscountAmount,
                MinOrderValue = voucher.MinOrderValue,
            };

            return ApiResponse<object>.SuccessResponse(response, "Áp dụng mã giảm giá thành công");
        }
    }
}
