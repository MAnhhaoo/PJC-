using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TourismApp.Models
{
    public class Restaurant : INotifyPropertyChanged
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
        public DateTime? PremiumExpireDate { get; set; }
        public List<Narration> Narrations { get; set; } = new List<Narration>();
        public double Distance { get; set; }

        // --- PHẦN THÊM MỚI ĐỂ HẾT LỖI ĐỎ ---
        private bool _isPlaying;
        public bool IsPlaying
        {
            get => _isPlaying;
            set
            {
                if (_isPlaying != value)
                {
                    _isPlaying = value;
                    OnPropertyChanged(); // Thông báo cho XAML cập nhật nút
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        // ------------------------------------
    }
}