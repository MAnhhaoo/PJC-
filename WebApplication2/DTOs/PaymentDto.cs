namespace WebApplication2.DTOs
{
    public class PaymentDto
    {
        public int PaymentId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentType { get; set; }
        public DateTime PaymentDate { get; set; }
        public string Status { get; set; }
    }
}
