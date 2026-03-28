namespace BE.DTOs.Order
{
    public class OrderDetailResponse
    {
        // Thông tin đơn hàng
        public int OrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public int Status { get; set; }
        public int PaymentMethod { get; set; }
        public int PaymentStatus { get; set; }

        // Thông tin khách hàng
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string Address { get; set; } = null!;
        public string? ProvinceName { get; set; }
        public string? DistrictName { get; set; }
        public string? WardName { get; set; }
        public string? Note { get; set; }

        // Thông tin giá
        public decimal Subtotal { get; set; }
        public string? VoucherCode { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal TotalMoney { get; set; }

        // Danh sách sản phẩm
        public List<OrderItemDetail> Items { get; set; } = new();
    }

    public class OrderItemDetail
    {
        public int OrderItemId { get; set; }
        public string ProductName { get; set; } = null!;
        public string? Color { get; set; }
        public string? Size { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal TotalMoney { get; set; }
    }
}
