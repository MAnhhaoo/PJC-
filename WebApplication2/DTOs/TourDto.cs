namespace WebApplication2.DTOs
{
    public class TourDto
    {
        public int TourId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Image { get; set; }
        public bool IsActive { get; set; }
        public decimal Price { get; set; }
        public bool IsPurchased { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<TourPOIDto> POIs { get; set; } = new();
    }

    public class TourPOIDto
    {
        public int TourPOIId { get; set; }
        public int RestaurantId { get; set; }
        public string RestaurantName { get; set; } = string.Empty;
        public string? RestaurantAddress { get; set; }
        public string? RestaurantImage { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int OrderIndex { get; set; }
        public List<NarrationDto> Narrations { get; set; } = new();
    }

    public class TourCreateDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Image { get; set; }
        public decimal Price { get; set; }
        public List<TourPOICreateDto> POIs { get; set; } = new();
    }

    public class TourPOICreateDto
    {
        public int RestaurantId { get; set; }
        public int OrderIndex { get; set; }
    }
}
