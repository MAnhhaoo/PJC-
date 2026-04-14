using TourismApp.Models;

namespace TourismApp.Models
{
    public class Tour
    {
        public int TourId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Image { get; set; }
        public bool IsActive { get; set; }
        public decimal Price { get; set; }
        public bool IsPurchased { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<TourPOI> POIs { get; set; } = new();
    }

    public class TourPOI
    {
        public int TourPOIId { get; set; }
        public int TourId { get; set; }
        public int RestaurantId { get; set; }
        public string RestaurantName { get; set; } = string.Empty;
        public string? RestaurantAddress { get; set; }
        public string? RestaurantImage { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int OrderIndex { get; set; }
        public List<Narration> Narrations { get; set; } = new();
    }
}
