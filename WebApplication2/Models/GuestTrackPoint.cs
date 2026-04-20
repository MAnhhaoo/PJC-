using System.ComponentModel.DataAnnotations;

namespace WebApplication2.Models
{
    public class GuestTrackPoint
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string DeviceId { get; set; } = "";

        public string SessionId { get; set; } = "";

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public DateTime RecordedAt { get; set; } = DateTime.Now;
    }
}
