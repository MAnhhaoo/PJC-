using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication2.Models
{
    public class TourTrackPoint
    {
        [Key]
        public int Id { get; set; }

        public int? UserId { get; set; }
        [ForeignKey("UserId")]
        public User? User { get; set; }

        public int TourId { get; set; }
        [ForeignKey("TourId")]
        public Tour Tour { get; set; } = null!;

        public string SessionId { get; set; } = string.Empty;

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public string? GuestLabel { get; set; }

        public DateTime RecordedAt { get; set; } = DateTime.Now;
    }
}
