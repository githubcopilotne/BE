namespace BE.DTOs.Product
{
    public class ClientProductItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public decimal UnitPrice { get; set; }
        public string? ImageUrl { get; set; }
        public double Rating { get; set; }
        public int ReviewCount { get; set; }
        public string Slug { get; set; } = null!;
    }
}
