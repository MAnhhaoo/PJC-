using System;
using System.Collections.Generic;
using System.Text;
namespace TourismApp.Models
{
    public class Restaurant
    {
        public int RestaurantId { get; set; }

        public string Name { get; set; }

        public string Address { get; set; }

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public string Description { get; set; }

        public string Image { get; set; }

        public bool IsPremium { get; set; }
        public bool IsActive { get; set; } = true;

        public int PremiumLevel { get; set; } = 0;

        public bool IsApproved { get; set; }

        //public double Rating { get; set; }

        public DateTime? PremiumExpireDate { get; set; }

        // Thêm dòng này vào class Restaurant của App Mobile
        public List<Narration> Narrations { get; set; } = new List<Narration>();

        public double Distance { get; set; }

    }
}