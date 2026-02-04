using WebApplication2.Models;
using BCrypt.Net;
namespace WebApplication2.Data
{
    public static class DbInitializer
    {
        public static void Seed(AppDbContext context)
        {
            if (context.Users.Any())
                return;

            var admin = new User
            {
                Email = "admin@gmail.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                FullName = "Admin",
                Role = "Admin"
            };

            var users = new List<User>
            {
                new User
                {
                    Email = "user1@gmail.com",
                    PasswordHash = "123456",
                    FullName = "Nguyen Van A",
                    Role = "User",
                    UserLevel = 1,
                    Status = "Active",
                    CreatedAt = DateTime.Now
                }
            };

            context.Users.Add(admin);
            context.Users.AddRange(users);
            context.SaveChanges();

            var payments = new List<Payment>
            {
                new Payment
                {
                    UserId = users[0].UserId,
                    Amount = 50000,
                    PaymentType = "Cash",
                    PaymentDate = DateTime.Now,
                    Status = "Success"
                }
            };

            context.Payments.AddRange(payments);
            context.SaveChanges();
        }
    }
}
