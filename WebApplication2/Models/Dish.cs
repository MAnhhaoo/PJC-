using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WebApplication2.Models;

public class Dish
{
    [Key]
    public int DishId { get; set; }

    [ForeignKey("Restaurant")]
    public int RestaurantId { get; set; }
    public Restaurant Restaurant { get; set; }

    public string Name { get; set; }
    public string ImageUrl { get; set; }
    public bool IsActive { get; set; }

    public ICollection<Narration> Narrations { get; set; }

    // ⭐ THÊM DÒNG NÀY
    public ICollection<DishHistory> DishHistories { get; set; }
}
