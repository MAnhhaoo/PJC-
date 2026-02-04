using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication2.Models
{
    public class RestaurantRegistrationHistory
    {
        [Key]
        public int RegistrationHistoryId { get; set; }

        [ForeignKey("Restaurant")]
        public int RestaurantId { get; set; }
        public Restaurant Restaurant { get; set; }

        [ForeignKey("Owner")]
        public int OwnerId { get; set; }
        public User Owner { get; set; }

        public string Action { get; set; }   // REGISTER / APPROVE / REJECT / UPDATE
        public DateTime ActionDate { get; set; }
        public string Note { get; set; }
    }
}
