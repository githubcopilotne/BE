namespace BE.DTOs.Order
{
    public class ConfirmOrderRequest
    {
        public List<int> ConfirmedItemIds { get; set; } = new();
    }
}
