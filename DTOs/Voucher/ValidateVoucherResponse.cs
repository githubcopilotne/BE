namespace BE.DTOs.Voucher
{
    public class ValidateVoucherResponse
    {
        public int VoucherId { get; set; }
        public string VoucherCode { get; set; } = null!;
        public int DiscountType { get; set; }     // 1: giảm %, 2: giảm tiền trực tiếp
        public decimal DiscountValue { get; set; }
        public decimal? MaxDiscountAmount { get; set; }
        public decimal? MinOrderValue { get; set; }
    }
}
