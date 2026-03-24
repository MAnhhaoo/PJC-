using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication2.Models
{
    public class Narration
    {
        [Key]
        public int NarrationId { get; set; }

        // Liên kết với Dish (Cho phép Null nếu là thuyết minh Nhà hàng)
        public int? DishId { get; set; }
        [ForeignKey("DishId")]
        public Dish? Dish { get; set; }

        // ⭐ THÊM LIÊN KẾT VỚI RESTAURANT (Cho phép Null nếu là thuyết minh Món ăn)
        public int? RestaurantId { get; set; }
        [ForeignKey("RestaurantId")]
        public Restaurant? Restaurant { get; set; }

        [Required]
        public int LanguageId { get; set; }
        [ForeignKey("LanguageId")]
        public Language Language { get; set; }

        public string TextContent { get; set; }
        public string AudioUrl { get; set; }

    }
}