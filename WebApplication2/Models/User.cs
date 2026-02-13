using System.ComponentModel.DataAnnotations;

namespace WebApplication2.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        public string FullName { get; set; }

        public string? Phone { get; set; }     // thêm
        public string? Address { get; set; }

        public string Role { get; set; }      // Admin / Restaurant / User
        public int UserLevel { get; set; }    // 0 = Thường, 1 = VIP

        public DateTime CreatedAt { get; set; }
        public string Status { get; set; }

        // Navigation
        public ICollection<Restaurant> Restaurants { get; set; }
        public ICollection<LocationHistory> LocationHistories { get; set; }
        public ICollection<DishHistory> DishHistories { get; set; }
        public ICollection<Payment> Payments { get; set; }
        public ICollection<Review> Reviews { get; set; }

    }
}
