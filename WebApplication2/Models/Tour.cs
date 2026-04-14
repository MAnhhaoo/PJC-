using System.ComponentModel.DataAnnotations;

namespace WebApplication2.Models
{
    public class Tour
    {
        [Key]
        public int TourId { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string? Image { get; set; }

        public bool IsActive { get; set; } = true;

        public decimal Price { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        public ICollection<TourPOI> TourPOIs { get; set; } = new List<TourPOI>();
    }
}
