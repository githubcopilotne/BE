namespace BE.DTOs.Cart
{
    public class CartItemDto
    {
        public int VariantId { get; set; }
        public string ProductName { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string ImageUrl { get; set; } = null!;
        public string? Color { get; set; }
        public string? Size { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public int StockQuantity { get; set; }
    }
}
