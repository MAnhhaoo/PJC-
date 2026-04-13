using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication2.Models
{
    public class TourPOI
    {
        [Key]
        public int TourPOIId { get; set; }

        [Required]
        public int TourId { get; set; }

        [ForeignKey("TourId")]
        public Tour Tour { get; set; } = null!;

        [Required]
        public int RestaurantId { get; set; }

        [ForeignKey("RestaurantId")]
        public Restaurant Restaurant { get; set; } = null!;

        /// <summary>
        /// Thứ tự của POI trong tour (1, 2, 3...)
        /// </summary>
        public int OrderIndex { get; set; }
    }
}
