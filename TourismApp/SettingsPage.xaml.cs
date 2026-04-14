using TourismApp.Services;

namespace TourismApp;

public partial class SettingsPage : ContentPage
{
    private readonly OfflineSyncService _offlineSyncService;
    private readonly LanguageService _lang;

    public SettingsPage(OfflineSyncService offlineSyncService, LanguageService languageService)
    {
        InitializeComponent();
        _offlineSyncService = offlineSyncService;
        _lang = languageService;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Title = _lang["SettingsTitle"];
        ApplyLocalization();

        // Load GPS settings
        var radius = Preferences.Default.Get("geofence_radius", 5.0);
        var movement = Preferences.Default.Get("min_movement", 3.0);
        sliderRadius.Value = radius;
        sliderMovement.Value = movement;
        lblRadius.Text = $"{radius:F0} m";
        lblMovement.Text = $"{movement:F0} m";

        // Load server IP
        entryServerIp.Text = Preferences.Default.Get("server_ip", "192.168.1.8");

        // Show last sync time
        var lastSync = Preferences.Default.Get("last_offline_sync", "");
        lblLastSync.Text = string.IsNullOrEmpty(lastSync) ? _lang["NeverSynced"] : string.Format(_lang["LastSyncPrefix"], lastSync);

        // Load anonymous mode
        var anonymous = Preferences.Default.Get("anonymous_mode", false);
        switchAnonymous.IsToggled = anonymous;
        UpdateAnonymousLabel(anonymous);
    }

    private void ApplyLocalization()
    {
        lblSettingsTitle.Text = _lang["Settings"];
        lblProfileLabel.Text = _lang["Profile"];
        lblViewEditProfile.Text = _lang["ViewEditProfile"];
        lblAnonymousHeader.Text = _lang["AnonymousMode"];
        lblAnonymousLabel.Text = _lang["AnonymousModeLabel"];
        lblAnonymousDesc.Text = _lang["AnonymousDesc"];
        lblGpsHeader.Text = _lang["GpsSettings"];
        lblDetectionRadius.Text = _lang["DetectionRadius"];
        lblDetectionRadiusDesc.Text = _lang["DetectionRadiusDesc"];
        lblMovementDistance.Text = _lang["MovementDistance"];
        lblMovementDistanceDesc.Text = _lang["MovementDistanceDesc"];
        btnResetGps.Text = _lang["ResetDefault"];
        lblServerHeader.Text = _lang["ServerConnection"];
        lblServerIP.Text = _lang["ServerIP"];
        lblServerIPDesc.Text = _lang["ServerIPDesc"];
        lblOfflineHeader.Text = _lang["OfflineData"];
        lblDownloadDesc.Text = _lang["DownloadOfflineDesc"];
        btnSync.Text = _lang["SyncOffline"];
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

    private async void OnViewProfile(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(ProfilePage));
    }

    private void OnAnonymousToggled(object sender, ToggledEventArgs e)
    {
        Preferences.Default.Set("anonymous_mode", e.Value);
        UpdateAnonymousLabel(e.Value);
    }

    private void UpdateAnonymousLabel(bool isAnonymous)
    {
        if (isAnonymous)
        {
            var label = Preferences.Default.Get("guest_label", "");
            if (string.IsNullOrEmpty(label))
            {
                label = "Guest_" + Guid.NewGuid().ToString("N")[..8].ToUpper();
                Preferences.Default.Set("guest_label", label);
            }
            lblAnonymousStatus.Text = string.Format(_lang["AnonymousOn"], label);
            lblAnonymousStatus.TextColor = Color.FromArgb("#4CAF50");
        }
        else
        {
            lblAnonymousStatus.Text = _lang["AnonymousOff"];
            lblAnonymousStatus.TextColor = Color.FromArgb("#999999");
        }
    }

    private async void OnSyncOfflineClicked(object sender, EventArgs e)
    {
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
        {
            await DisplayAlert(_lang["Error"], _lang["NeedNetwork"], _lang["OK"]);
            return;
        }

        btnSync.IsEnabled = false;
        lblSyncStatus.Text = _lang["SyncingRestaurants"];

        try
        {
            await _offlineSyncService.SyncRestaurantsAsync();
            lblSyncStatus.Text = _lang["SyncingTours"];
            await _offlineSyncService.SyncToursAsync();
            lblSyncStatus.Text = _lang["DownloadingAudioFiles"];
            await _offlineSyncService.DownloadAudioFilesAsync();

            var now = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
            Preferences.Default.Set("last_offline_sync", now);
            lblLastSync.Text = string.Format(_lang["LastSyncPrefix"], now);
            lblSyncStatus.Text = _lang["SyncSuccess"];
        }
        catch (Exception ex)
        {
            lblSyncStatus.Text = string.Format(_lang["SyncError"], ex.Message);
        }
        finally
        {
            btnSync.IsEnabled = true;
        }
    }
}
