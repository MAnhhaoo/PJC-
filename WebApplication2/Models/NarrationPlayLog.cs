using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication2.Models
{
    public class NarrationPlayLog
    {
        [Key]
        public int Id { get; set; }

        public int? UserId { get; set; }
        [ForeignKey("UserId")]
        public User? User { get; set; }

        public int RestaurantId { get; set; }
        [ForeignKey("RestaurantId")]
        public Restaurant Restaurant { get; set; } = null!;

        public int? TourId { get; set; }
        public int? NarrationId { get; set; }
        public string? LanguageCode { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public DateTime PlayedAt { get; set; } = DateTime.Now;
    }
}
