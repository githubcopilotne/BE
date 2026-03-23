namespace BE.DTOs.Order
{
    public class UserOrderListResponse
    {
        public int OrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public int Status { get; set; }
        public decimal TotalMoney { get; set; }
        public int PaymentMethod { get; set; }
        public int PaymentStatus { get; set; }
        public int TotalItems { get; set; }
    }
}
