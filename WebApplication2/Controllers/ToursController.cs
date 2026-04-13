using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication2.Data;
using WebApplication2.DTOs;
using WebApplication2.Models;

namespace WebApplication2.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ToursController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ToursController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/tours
        [HttpGet]
        public async Task<IActionResult> GetTours()
        {
            var tours = await _context.Tours
                .Where(t => t.IsActive)
                .Include(t => t.TourPOIs)
                    .ThenInclude(tp => tp.Restaurant)
                        .ThenInclude(r => r.Narrations)
                            .ThenInclude(n => n.Language)
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new TourDto
                {
                    TourId = t.TourId,
                    Name = t.Name,
                    Description = t.Description,
                    Image = t.Image,
                    IsActive = t.IsActive,
                    CreatedAt = t.CreatedAt,
                    POIs = t.TourPOIs
                        .OrderBy(tp => tp.OrderIndex)
                        .Select(tp => new TourPOIDto
                        {
                            TourPOIId = tp.TourPOIId,
                            RestaurantId = tp.RestaurantId,
                            RestaurantName = tp.Restaurant.Name ?? "",
                            RestaurantAddress = tp.Restaurant.Address,
                            RestaurantImage = tp.Restaurant.Image,
                            Latitude = tp.Restaurant.Latitude,
                            Longitude = tp.Restaurant.Longitude,
                            OrderIndex = tp.OrderIndex,
                            Narrations = tp.Restaurant.Narrations
                                .Where(n => n.RestaurantId != null)
                                .Select(n => new NarrationDto
                                {
                                    NarrationId = n.NarrationId,
                                    TextContent = n.TextContent,
                                    AudioUrl = string.IsNullOrEmpty(n.AudioUrl) ? "" :
                                               n.AudioUrl.Replace("audios/", "").TrimStart('/'),
                                    Language = new LanguageDto
                                    {
                                        Code = n.Language.Code,
                                        Name = n.Language.Name
                                    }
                                }).ToList()
                        }).ToList()
                })
                .ToListAsync();

            return Ok(tours);
        }

        // GET: api/tours/all (admin — includes inactive)
        [HttpGet("all")]
        [Authorize]
        public async Task<IActionResult> GetAllTours()
        {
            var tours = await _context.Tours
                .Include(t => t.TourPOIs)
                    .ThenInclude(tp => tp.Restaurant)
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new TourDto
                {
                    TourId = t.TourId,
                    Name = t.Name,
                    Description = t.Description,
                    Image = t.Image,
                    IsActive = t.IsActive,
                    CreatedAt = t.CreatedAt,
                    POIs = t.TourPOIs
                        .OrderBy(tp => tp.OrderIndex)
                        .Select(tp => new TourPOIDto
                        {
                            TourPOIId = tp.TourPOIId,
                            RestaurantId = tp.RestaurantId,
                            RestaurantName = tp.Restaurant.Name ?? "",
                            RestaurantAddress = tp.Restaurant.Address,
                            Latitude = tp.Restaurant.Latitude,
                            Longitude = tp.Restaurant.Longitude,
                            OrderIndex = tp.OrderIndex
                        }).ToList()
                })
                .ToListAsync();

            return Ok(tours);
        }

        // GET: api/tours/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTour(int id)
        {
            var tour = await _context.Tours
                .Include(t => t.TourPOIs)
                    .ThenInclude(tp => tp.Restaurant)
                        .ThenInclude(r => r.Narrations)
                            .ThenInclude(n => n.Language)
                .Where(t => t.TourId == id)
                .Select(t => new TourDto
                {
                    TourId = t.TourId,
                    Name = t.Name,
                    Description = t.Description,
                    Image = t.Image,
                    IsActive = t.IsActive,
                    CreatedAt = t.CreatedAt,
                    POIs = t.TourPOIs
                        .OrderBy(tp => tp.OrderIndex)
                        .Select(tp => new TourPOIDto
                        {
                            TourPOIId = tp.TourPOIId,
                            RestaurantId = tp.RestaurantId,
                            RestaurantName = tp.Restaurant.Name ?? "",
                            RestaurantAddress = tp.Restaurant.Address,
                            RestaurantImage = tp.Restaurant.Image,
                            Latitude = tp.Restaurant.Latitude,
                            Longitude = tp.Restaurant.Longitude,
                            OrderIndex = tp.OrderIndex,
                            Narrations = tp.Restaurant.Narrations
                                .Where(n => n.RestaurantId != null)
                                .Select(n => new NarrationDto
                                {
                                    NarrationId = n.NarrationId,
                                    TextContent = n.TextContent,
                                    AudioUrl = string.IsNullOrEmpty(n.AudioUrl) ? "" :
                                               n.AudioUrl.Replace("audios/", "").TrimStart('/'),
                                    Language = new LanguageDto
                                    {
                                        Code = n.Language.Code,
                                        Name = n.Language.Name
                                    }
                                }).ToList()
                        }).ToList()
                })
                .FirstOrDefaultAsync();

            if (tour == null) return NotFound();
            return Ok(tour);
        }

        // POST: api/tours
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateTour([FromBody] TourCreateDto dto)
        {
            var tour = new Tour
            {
                Name = dto.Name,
                Description = dto.Description,
                Image = dto.Image,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.Tours.Add(tour);
            await _context.SaveChangesAsync();

            // Add POIs
            if (dto.POIs != null)
            {
                foreach (var poi in dto.POIs)
                {
                    _context.TourPOIs.Add(new TourPOI
                    {
                        TourId = tour.TourId,
                        RestaurantId = poi.RestaurantId,
                        OrderIndex = poi.OrderIndex
                    });
                }
                await _context.SaveChangesAsync();
            }

            return Ok(new { tour.TourId });
        }

        // PUT: api/tours/{id}
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateTour(int id, [FromBody] TourCreateDto dto)
        {
            var tour = await _context.Tours
                .Include(t => t.TourPOIs)
                .FirstOrDefaultAsync(t => t.TourId == id);

            if (tour == null) return NotFound();

            tour.Name = dto.Name;
            tour.Description = dto.Description;
            tour.Image = dto.Image;

            // Replace POIs
            _context.TourPOIs.RemoveRange(tour.TourPOIs);

            if (dto.POIs != null)
            {
                foreach (var poi in dto.POIs)
                {
                    _context.TourPOIs.Add(new TourPOI
                    {
                        TourId = tour.TourId,
                        RestaurantId = poi.RestaurantId,
                        OrderIndex = poi.OrderIndex
                    });
                }
            }

            await _context.SaveChangesAsync();
            return Ok();
        }

        // PATCH: api/tours/{id}/toggle
        [HttpPatch("{id}/toggle")]
        [Authorize]
        public async Task<IActionResult> ToggleTour(int id)
        {
            var tour = await _context.Tours.FindAsync(id);
            if (tour == null) return NotFound();

            tour.IsActive = !tour.IsActive;
            await _context.SaveChangesAsync();

            return Ok(new { tour.IsActive });
        }

        // DELETE: api/tours/{id}
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteTour(int id)
        {
            var tour = await _context.Tours
                .Include(t => t.TourPOIs)
                .FirstOrDefaultAsync(t => t.TourId == id);

            if (tour == null) return NotFound();

            _context.Tours.Remove(tour);
            await _context.SaveChangesAsync();

            return Ok();
        }
    }
}
