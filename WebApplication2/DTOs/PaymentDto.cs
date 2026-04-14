namespace WebApplication2.DTOs
{
    public class PaymentDto
    {
        public int PaymentId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentType { get; set; }
        public DateTime PaymentDate { get; set; }
        public string Status { get; set; }
        public int? TourId { get; set; }
        public int? RestaurantId { get; set; }
        public string? PaymentMethod { get; set; }
    }

    public class TourPurchaseRequest
    {
        public int TourId { get; set; }
        public string? PaymentMethod { get; set; }
        public string? TransactionId { get; set; }
    }

    public class RestaurantPaymentRequest
    {
        public int RestaurantId { get; set; }
        public decimal Amount { get; set; }
        public string? PaymentMethod { get; set; }
        public string? TransactionId { get; set; }
        public string? PlanType { get; set; } // "Normal" or "Premium"
    }

    // Webhook payload from bank monitoring services (SePay, Casso, etc.)
    public class BankWebhookPayload
    {
        public int Id { get; set; }
        public string Gateway { get; set; } = "";
        public string TransactionDate { get; set; } = "";
        public string AccountNumber { get; set; } = "";
        public string? Code { get; set; }
        public string Content { get; set; } = "";
        public string TransferType { get; set; } = "";
        public decimal TransferAmount { get; set; }
        public decimal Accumulated { get; set; }
        public string? SubAccount { get; set; }
        public string? ReferenceCode { get; set; }
        public string? Description { get; set; }
    }
}
