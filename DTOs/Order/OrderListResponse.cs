namespace BE.DTOs.Order
{
    public class OrderListResponse
    {
        public int OrderId { get; set; }
        public string FullName { get; set; } = null!;
        public decimal TotalMoney { get; set; }
        public int PaymentMethod { get; set; }
        public int PaymentStatus { get; set; }
        public int Status { get; set; }
        public DateTime OrderDate { get; set; }
    }
}
