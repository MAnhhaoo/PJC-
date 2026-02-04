using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WebApplication2.Models;

public class Restaurant
{
    [Key]
    public int RestaurantId { get; set; }

    public string Name { get; set; }
    public string Address { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Description { get; set; }

    [ForeignKey("Owner")]
    public int OwnerId { get; set; }
    public User Owner { get; set; }

    public bool IsApproved { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation
    public ICollection<Dish> Dishes { get; set; }
    public ICollection<Review> Reviews { get; set; }

    // ⭐ THÊM DÒNG NÀY
    public ICollection<LocationHistory> LocationHistories { get; set; }
}
