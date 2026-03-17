namespace BE.DTOs.Cart
{
    public class UpdateCartItemRequest
    {
        public int VariantId { get; set; }
        public int Quantity { get; set; }
    }
}
