namespace BE.DTOs.Order
{
    public class CreateOrderRequest
    {
        public string FullName { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string Address { get; set; } = null!;
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
