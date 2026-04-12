namespace TourismApp;

public partial class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Load GPS settings
        var radius = Preferences.Default.Get("geofence_radius", 5.0);
        var movement = Preferences.Default.Get("min_movement", 3.0);
        sliderRadius.Value = radius;
        sliderMovement.Value = movement;
        lblRadius.Text = $"{radius:F0} m";
        lblMovement.Text = $"{movement:F0} m";

        // Load server IP
        entryServerIp.Text = Preferences.Default.Get("server_ip", "192.168.1.12");
    }

    private void OnRadiusChanged(object sender, ValueChangedEventArgs e)
    {
        var value = Math.Round(e.NewValue);
        lblRadius.Text = $"{value:F0} m";
        Preferences.Default.Set("geofence_radius", value);
    }

    private void OnMovementChanged(object sender, ValueChangedEventArgs e)
    {
        var value = Math.Round(e.NewValue);
        lblMovement.Text = $"{value:F0} m";
        Preferences.Default.Set("min_movement", value);
    }

    private void OnResetGpsSettings(object sender, EventArgs e)
    {
        sliderRadius.Value = 5;
        sliderMovement.Value = 3;
        Preferences.Default.Set("geofence_radius", 5.0);
        Preferences.Default.Set("min_movement", 3.0);
        lblRadius.Text = "5 m";
        lblMovement.Text = "3 m";
    }

    private void OnServerIpChanged(object sender, TextChangedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(e.NewTextValue))
        {
            Preferences.Default.Set("server_ip", e.NewTextValue.Trim());
        }
    }
}
