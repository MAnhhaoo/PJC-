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
        public DbSet<Tour> Tours { get; set; }
        public DbSet<TourPOI> TourPOIs { get; set; }
        public DbSet<NarrationPlayLog> NarrationPlayLogs { get; set; }
        public DbSet<TourTrackPoint> TourTrackPoints { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ===== Payment =====
            modelBuilder.Entity<Payment>()
                .Property(p => p.Amount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Tour)
                .WithMany()
                .HasForeignKey(p => p.TourId)
                .OnDelete(DeleteBehavior.NoAction);

            // ===== Tour =====
            modelBuilder.Entity<Tour>()
                .Property(t => t.Price)
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

            // ===== Tour =====
            modelBuilder.Entity<TourPOI>()
                .HasOne(tp => tp.Tour)
                .WithMany(t => t.TourPOIs)
                .HasForeignKey(tp => tp.TourId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TourPOI>()
                .HasOne(tp => tp.Restaurant)
                .WithMany()
                .HasForeignKey(tp => tp.RestaurantId)
                .OnDelete(DeleteBehavior.NoAction);

            // ===== NarrationPlayLog =====
            modelBuilder.Entity<NarrationPlayLog>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<NarrationPlayLog>()
                .HasOne(n => n.Restaurant)
                .WithMany()
                .HasForeignKey(n => n.RestaurantId)
                .OnDelete(DeleteBehavior.NoAction);

            // ===== TourTrackPoint =====
            modelBuilder.Entity<TourTrackPoint>()
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<TourTrackPoint>()
                .HasOne(t => t.Tour)
                .WithMany()
                .HasForeignKey(t => t.TourId)
                .OnDelete(DeleteBehavior.NoAction);

            base.OnModelCreating(modelBuilder);
        }


    }


}
