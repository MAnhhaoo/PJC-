namespace TourismApp.Controls;

public partial class CustomBottomBar : ContentView
{
    public static readonly BindableProperty ActiveTabProperty =
        BindableProperty.Create(nameof(ActiveTab), typeof(int), typeof(CustomBottomBar), 0,
            propertyChanged: OnActiveTabChanged);

    public int ActiveTab
    {
        get => (int)GetValue(ActiveTabProperty);
        set => SetValue(ActiveTabProperty, value);
    }

    public CustomBottomBar()
    {
        InitializeComponent();
    }

    protected override void OnPropertyChanged(string propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if (propertyName == nameof(ActiveTab))
            UpdateActiveTab(ActiveTab);
    }

    static void OnActiveTabChanged(BindableObject bindable, object oldValue, object newValue)
    {
        ((CustomBottomBar)bindable).UpdateActiveTab((int)newValue);
    }

    void UpdateActiveTab(int index)
    {
        var activeColor = Color.FromArgb("#FF5722");
        var inactiveColor = Colors.Gray;

        LabelHome.TextColor = index == 0 ? activeColor : inactiveColor;
        LabelExplore.TextColor = index == 1 ? activeColor : inactiveColor;
        LabelProfile.TextColor = index == 2 ? activeColor : inactiveColor;
        LabelSettings.TextColor = index == 3 ? activeColor : inactiveColor;
    }

    async void OnHomeTapped(object sender, EventArgs e)
    {
        SwitchTab(0);
    }

    async void OnExploreTapped(object sender, EventArgs e)
    {
        SwitchTab(1);
    }

    async void OnProfileTapped(object sender, EventArgs e)
    {
        SwitchTab(2);
    }

    async void OnSettingsTapped(object sender, EventArgs e)
    {
        SwitchTab(3);
    }

    async void OnScanTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(QRScannerPage));
    }

    void SwitchTab(int index)
    {
        if (Shell.Current?.CurrentItem != null && index < Shell.Current.CurrentItem.Items.Count)
        {
            Shell.Current.CurrentItem.CurrentItem = Shell.Current.CurrentItem.Items[index];
        }
    }
}
