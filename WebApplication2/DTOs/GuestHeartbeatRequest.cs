namespace WebApplication2.DTOs;

public class GuestHeartbeatRequest
{
    public string DeviceId { get; set; } = "";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public class HeartbeatRequest
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}
