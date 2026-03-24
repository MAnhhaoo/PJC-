namespace WebApplication2.DTOs
{
    public class DishWithNarrationDto
    {
        // Thông tin món ăn
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string Description { get; set; }
        public IFormFile ImageFile { get; set; }

        // Thông tin thuyết minh (Narration)
        public int? LanguageId { get; set; }
        public string TextContent { get; set; }
        public IFormFile AudioFile { get; set; }
    }
}
