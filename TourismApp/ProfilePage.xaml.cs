using TourismApp.Services;
using TourismApp.Models;

namespace TourismApp;

public partial class ProfilePage : ContentPage
{
    private readonly UserService _userService;
    private User _user;

    public ProfilePage(UserService userService)
    {
        InitializeComponent();
        _userService = userService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        _user = await _userService.GetMeAsync();

        lblEmail.Text = _user.Email;
        lblFullName.Text = _user.FullName;
        lblPhone.Text = _user.Phone;
        lblAddress.Text = _user.Address;
    }

    private async void OnEditClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(EditProfilePage));
    }
}