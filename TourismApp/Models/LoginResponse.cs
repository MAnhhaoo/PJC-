using System;
using System.Collections.Generic;
using System.Text;
namespace TourismApp.Models
{
    public class LoginResponse
    {
        public string FullName { get; set; }
        public string Token { get; set; }
        public string Role { get; set; }

        public bool HasRestaurant { get; set; }
    }
}
