using System.ComponentModel.DataAnnotations;

namespace WebApplication2.Models
{
    public class GuestSession
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string DeviceId { get; set; } = "";

        public DateTime LastActiveAt { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
}
