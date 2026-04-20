using System.Net.Http.Json;

namespace TourismApp.Services;

public class HeartbeatService
{
    private readonly HttpClient _httpClient;
    private CancellationTokenSource? _cts;
    private bool _isRunning;

    public HeartbeatService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public void Start()
    {
        if (_isRunning) return;
        _isRunning = true;
        _cts = new CancellationTokenSource();
        _ = RunAsync(_cts.Token);
    }

    public void Stop()
    {
        _isRunning = false;
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    private async Task RunAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                // Get current GPS location for heartbeat
                double lat = 0, lng = 0;
                try
                {
                    var location = await Geolocation.Default.GetLocationAsync(new GeolocationRequest
                    {
                        DesiredAccuracy = GeolocationAccuracy.Medium,
                        Timeout = TimeSpan.FromSeconds(5)
                    }, ct);
                    if (location != null)
                    {
                        lat = location.Latitude;
                        lng = location.Longitude;
                    }
                }
                catch { }

                var token = await SecureStorage.GetAsync("auth_token");
                if (!string.IsNullOrEmpty(token))
                {
                    // Registered user heartbeat with location
                    await _httpClient.PostAsJsonAsync("api/users/heartbeat",
                        new { Latitude = lat, Longitude = lng }, ct);
                }
                else
                {
                    // Guest heartbeat with device ID and location
                    var deviceId = GetDeviceId();
                    await _httpClient.PostAsJsonAsync("api/users/guest-heartbeat",
                        new { DeviceId = deviceId, Latitude = lat, Longitude = lng }, ct);
                }
            }
            catch
            {
                // Ignore errors — heartbeat is best-effort
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(2), ct);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }

    private static string GetDeviceId()
    {
        var id = Preferences.Default.Get("guest_device_id", "");
        if (string.IsNullOrEmpty(id))
        {
            id = Guid.NewGuid().ToString();
            Preferences.Default.Set("guest_device_id", id);
        }
        return id;
    }
}
