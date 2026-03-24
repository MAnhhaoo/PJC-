using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WebApplication2.Models;

public class Restaurant
{
    [Key]
    public int RestaurantId { get; set; }

    public string  Phone { get; set; }
    public string Name { get; set; }
    public string Address { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Description { get; set; }

    // iamge of restaurant 
    public string? Image { get; set; }

    public bool IsActive { get; set; } = true; 

    [ForeignKey("Owner")]
    public int OwnerId { get; set; }
    public User Owner { get; set; }

    public bool IsApproved { get; set; }
    public DateTime CreatedAt { get; set; }

    // ===== ⭐ THÊM PHẦN PREMIUM =====
    public bool IsPremium { get; set; } = false;
    public int PremiumLevel { get; set; } = 0; 
    public DateTime? PremiumExpireDate { get; set; }

    // Navigation
    public ICollection<Dish> Dishes { get; set; }
    public ICollection<Review> Reviews { get; set; }

    // ⭐ THÊM DÒNG NÀY
    public ICollection<LocationHistory> LocationHistories { get; set; }

    public ICollection<Narration> Narrations { get; set; }
}
