namespace BE.DTOs.Voucher
{
    public class ValidateVoucherResponse
    {
        public int VoucherId { get; set; }
        public string VoucherCode { get; set; } = null!;
        public int DiscountType { get; set; }     // 0: giảm tiền, 1: giảm %
        public decimal DiscountValue { get; set; }
    }
}
