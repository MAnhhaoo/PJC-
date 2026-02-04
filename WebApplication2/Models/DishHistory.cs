using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication2.Models
{
    public class DishHistory
    {
        [Key]
        public int DishHistoryId { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }
        public User User { get; set; }

        [ForeignKey("Dish")]
        public int DishId { get; set; }
        public Dish Dish { get; set; }

        public DateTime ListenedAt { get; set; }
    }
}
