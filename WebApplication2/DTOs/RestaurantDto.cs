namespace WebApplication2.DTOs
{
    public class RestaurantDto
    {
        public int RestaurantId { get; set; }
        public string? Phone { get; set; } // Thêm dấu ? để cho phép null
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Description { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? Image { get; set; }
        public bool IsPremium { get; set; }
        public bool IsActive { get; set; }
        public bool IsApproved { get; set; }
        public double BroadcastRadius { get; set; } = 50;
        public DateTime? PremiumExpireDate { get; set; }
        public List<NarrationDto> Narrations { get; set; } = new List<NarrationDto>();
    }
}
