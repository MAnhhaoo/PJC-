namespace TourismApp.Services
{
    public class GpsService
    {
        public async Task<Location?> GetCurrentLocation()
        {
            try
            {
                var request = new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(10));
                return await Geolocation.Default.GetLocationAsync(request);
            }
            catch (Exception) { return null; }
        }

        // Hàm tính khoảng cách giữa User và Nhà hàng (trả về mét)
        public double CalculateDistance(double userLat, double userLng, double resLat, double resLng)
        {
            return Location.CalculateDistance(userLat, userLng, resLat, resLng, DistanceUnits.Kilometers) * 1000;
        }
    }
}