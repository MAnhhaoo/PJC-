namespace WebApplication2.DTOs
{
    public class UpdateProfileDto
    {
        public string FullName { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }

        // 🔥 chỉ Admin mới được dùng
        public int? UserLevel { get; set; }
    }
}
