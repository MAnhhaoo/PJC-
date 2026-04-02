namespace BlazorApp1.Models
{
    public class NarrationDto
    {
        public int NarrationId { get; set; }
        public string? TextContent { get; set; } // Thêm ? ở đây
        public string? LanguageName { get; set; } // Thêm ? ở đây
        public string? AudioUrl { get; set; } // Thêm ? ở đây
        public int DishId { get; set; }
    }

    public class NarrationAdminDto : NarrationDto
    {
        public string? RestaurantName { get; set; }
        public int? RestaurantId { get; set; }
    }
}