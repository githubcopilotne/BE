namespace BE.DTOs.Product
{
    public class ClientProductDetail
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public string? Description { get; set; }
        public double Rating { get; set; }
        public int ReviewCount { get; set; }
        public string CategorySlug { get; set; } = string.Empty;

        public List<ProductImageDto> Images { get; set; } = new();
        public List<ProductVariantDto> Variants { get; set; } = new();
        public List<ProductReviewDto> Reviews { get; set; } = new();
    }

    public class ProductImageDto
    {
        public int ImageId { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public bool IsMain { get; set; }
    }

    public class ProductVariantDto
    {
        public int VariantId { get; set; }
        public string? Color { get; set; }
        public string? Size { get; set; }
        public int StockQuantity { get; set; }
    }

    public class ProductReviewDto
    {
        public string UserName { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
