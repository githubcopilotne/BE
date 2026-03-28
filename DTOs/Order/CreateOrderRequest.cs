namespace BE.DTOs.Order
{
    public class CreateOrderRequest
    {
        public string FullName { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string Address { get; set; } = null!;
        public int? ProvinceId { get; set; }
        public string? ProvinceName { get; set; }
        public int? DistrictId { get; set; }
        public string? DistrictName { get; set; }
        public string? WardCode { get; set; }
        public string? WardName { get; set; }
        public string? Note { get; set; }
        public int PaymentMethod { get; set; }  // 0: COD, 1: VNPay
        public int? VoucherId { get; set; }
        public List<OrderItemRequest> Items { get; set; } = new();
    }

    public class OrderItemRequest
    {
        public int VariantId { get; set; }
        public int Quantity { get; set; }
    }
}
