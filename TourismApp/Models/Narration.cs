namespace TourismApp.Models
{
    public class Narration
    {
        public int NarrationId { get; set; }
        public int? DishId { get; set; }
        public int? RestaurantId { get; set; }
        public int LanguageId { get; set; }
        public Language Language { get; set; } // Đảm bảo bạn đã có class Language trong App
        public string TextContent { get; set; }
        public string AudioUrl { get; set; }
        // Đường dẫn file âm thanh offline (nếu đã tải)
        public string LocalAudioPath { get; set; }
    }

    public class Language
    {
        public int LanguageId { get; set; }
        public string Name { get; set; }
        public string Code { get; set; } // Ví dụ: "vi", "en"
    }
}