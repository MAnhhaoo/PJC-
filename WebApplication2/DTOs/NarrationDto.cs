namespace WebApplication2.DTOs
{
    public class NarrationDto
    {
        public int NarrationId { get; set; }
        public int LanguageId { get; set; }
        public string TextContent { get; set; }
        public string AudioUrl { get; set; }
        public LanguageDto Language { get; set; }
    }
}