using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication2.Models
{
    public class Narration
    {
        [Key]
        public int NarrationId { get; set; }

        [ForeignKey("Dish")]
        public int DishId { get; set; }
        public Dish Dish { get; set; }

        [ForeignKey("Language")]
        public int LanguageId { get; set; }
        public Language Language { get; set; }

        public string TextContent { get; set; }
        public string AudioUrl { get; set; }
    }
}
