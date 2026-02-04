using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication2.Models
{
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }
        public User User { get; set; }

        public decimal Amount { get; set; }
        public string PaymentType { get; set; }
        public DateTime PaymentDate { get; set; }
        public string Status { get; set; }
    }
}
