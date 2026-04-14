using Microsoft.EntityFrameworkCore;
using TourismApp.Models;

namespace TourismApp.Services;

public class PoiDbContext : DbContext
{
    public DbSet<Restaurant> Restaurants { get; set; }
    public DbSet<Narration> Narrations { get; set; }
    public DbSet<Language> Languages { get; set; }
    public DbSet<Tour> Tours { get; set; }
    public DbSet<TourPOI> TourPOIs { get; set; }

    private string DbPath => Path.Combine(FileSystem.AppDataDirectory, "poi.db");

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite($"Filename={DbPath}");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Restaurant>(e =>
        {
            e.HasKey(r => r.RestaurantId);
            e.Property(r => r.RestaurantId).ValueGeneratedNever();
            e.Property(r => r.Name).IsRequired(false);
            e.Property(r => r.Address).IsRequired(false);
            e.Property(r => r.Description).IsRequired(false);
            e.Property(r => r.Image).IsRequired(false);
            e.Ignore(r => r.Distance);
            e.Ignore(r => r.IsPlaying);
            e.Ignore(r => r.IsNearest);
            e.Ignore(r => r.Narrations);
        });

        modelBuilder.Entity<Narration>(e =>
        {
            e.HasKey(n => n.NarrationId);
            e.Property(n => n.NarrationId).ValueGeneratedNever();
            e.Property(n => n.TextContent).IsRequired(false);
            e.Property(n => n.AudioUrl).IsRequired(false);
            e.Property(n => n.LocalAudioPath).IsRequired(false);
            e.Ignore(n => n.Language);
        });

        modelBuilder.Entity<Language>(e =>
        {
            e.HasKey(l => l.LanguageId);
            e.Property(l => l.LanguageId).ValueGeneratedNever();
            e.Property(l => l.Name).IsRequired(false);
            e.Property(l => l.Code).IsRequired(false);
        });

        modelBuilder.Entity<Tour>(e =>
        {
            e.HasKey(t => t.TourId);
            e.Property(t => t.TourId).ValueGeneratedNever();
            e.Property(t => t.Name).IsRequired(false);
            e.Property(t => t.Description).IsRequired(false);
            e.Property(t => t.Image).IsRequired(false);
            e.Ignore(t => t.POIs);
            e.Ignore(t => t.IsPurchased);
        });

        modelBuilder.Entity<TourPOI>(e =>
        {
            e.HasKey(tp => tp.TourPOIId);
            e.Property(tp => tp.TourPOIId).ValueGeneratedNever();
            e.Property(tp => tp.RestaurantName).IsRequired(false);
            e.Property(tp => tp.RestaurantAddress).IsRequired(false);
            e.Property(tp => tp.RestaurantImage).IsRequired(false);
            e.Ignore(tp => tp.Narrations);
        });
    }
}
