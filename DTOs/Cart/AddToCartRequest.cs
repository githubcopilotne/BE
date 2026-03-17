namespace BE.DTOs.Cart
{
    public class AddToCartRequest
    {
        public int VariantId { get; set; }
        public int Quantity { get; set; }
    }
}
