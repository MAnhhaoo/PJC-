using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication2.Models
{
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User User { get; set; }

        // ===== CỘT CŨ (GIỮ NGUYÊN) =====
        public decimal Amount { get; set; }

        public string PaymentType { get; set; }   // UserUpgrade / RestaurantRegistration / RestaurantPremium

        public DateTime PaymentDate { get; set; }

        public string Status { get; set; }        // Pending / Success / Failed


        // ===== CỘT MỚI (THÊM VÀO - NULLABLE) =====

        // Nếu payment liên quan tới nhà hàng cụ thể
        public int? RestaurantId { get; set; }

        [ForeignKey(nameof(RestaurantId))]
        public Restaurant? Restaurant { get; set; }

        // Momo / VNPAY / Stripe / Cash
        [MaxLength(50)]
        public string? PaymentMethod { get; set; }

        // Mã giao dịch từ cổng thanh toán
        [MaxLength(100)]
        public string? TransactionId { get; set; }

        // Ngày hết hạn gói VIP / Premium
        public DateTime? ExpireDate { get; set; }
    }
}
