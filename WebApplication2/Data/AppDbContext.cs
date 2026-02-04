using Microsoft.EntityFrameworkCore;
using WebApplication2.Models;

namespace WebApplication2.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Restaurant> Restaurants { get; set; }
        public DbSet<Dish> Dishes { get; set; }
        public DbSet<Narration> Narrations { get; set; }
        public DbSet<Language> Languages { get; set; }
        public DbSet<LocationHistory> LocationHistories { get; set; }
        public DbSet<DishHistory> DishHistories { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<SystemLog> SystemLogs { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ===== Payment =====
            modelBuilder.Entity<Payment>()
                .Property(p => p.Amount)
                .HasPrecision(18, 2);

            // ===== LocationHistory =====
            modelBuilder.Entity<LocationHistory>()
                .HasOne(l => l.User)
                .WithMany(u => u.LocationHistories)
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<LocationHistory>()
                .HasOne(l => l.Restaurant)
                .WithMany() // Restaurant KHÔNG có LocationHistories
                .HasForeignKey(l => l.RestaurantId)
                .OnDelete(DeleteBehavior.NoAction);

            // ===== DishHistory =====
            modelBuilder.Entity<DishHistory>()
                .HasOne(d => d.User)
                .WithMany(u => u.DishHistories)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<DishHistory>()
                .HasOne(d => d.Dish)
                .WithMany() // Dish KHÔNG có DishHistories
                .HasForeignKey(d => d.DishId)
                .OnDelete(DeleteBehavior.NoAction);

            // ===== Review =====
            modelBuilder.Entity<Review>()
                .HasOne(r => r.User)
                .WithMany(u => u.Reviews)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.Restaurant)
                .WithMany(res => res.Reviews)
                .HasForeignKey(r => r.RestaurantId)
                .OnDelete(DeleteBehavior.NoAction);

            base.OnModelCreating(modelBuilder);
        }


    }


}
