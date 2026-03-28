namespace BE.DTOs.Ghn
{
    public class ShippingFeeRequest
    {
        public int DistrictId { get; set; }
        public string WardCode { get; set; } = null!;
    }
}
