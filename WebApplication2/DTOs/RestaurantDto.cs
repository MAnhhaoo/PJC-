namespace WebApplication2.DTOs
{
    public class RestaurantDto
    {
        public int RestaurantId { get; set; }

        public string Name { get; set; }

        public string Address { get; set; }

        public string Description { get; set; }

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public string Image { get; set; }

        public bool IsPremium { get; set; }

        public bool IsApproved { get; set; }   // 🔥 thêm dòng này
    }
}
