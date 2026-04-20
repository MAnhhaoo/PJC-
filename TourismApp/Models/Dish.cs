using System;
using System.Collections.Generic;
using System.Text;

namespace TourismApp.Models
{
    public class Dish
    {
        public int DishId { get; set; }

        public int RestaurantId { get; set; }

        public string Name { get; set; }

        public string ImageUrl { get; set; }

        public string Image => ImageUrl;

        public bool IsActive { get; set; }
    }
}