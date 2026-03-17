namespace BE.DTOs.Cart
{
    public class SyncCartItem
    {
        public int VariantId { get; set; }
        public int Quantity { get; set; }
    }
    public class SyncCartRequest
    {
        public List<SyncCartItem> Items { get; set; } = new();
    }
}
