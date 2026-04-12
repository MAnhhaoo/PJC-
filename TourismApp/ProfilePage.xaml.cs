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

        try
        {
            _user = await _userService.GetMeAsync();

            if (_user != null)
            {
                lblEmail.Text = _user.Email;
                lblFullNameHeader.Text = _user.FullName; // Hiển thị tên ở Header
                lblPhone.Text = string.IsNullOrEmpty(_user.Phone) ? "Chưa cập nhật" : _user.Phone;
                lblAddress.Text = string.IsNullOrEmpty(_user.Address) ? "Chưa cập nhật" : _user.Address;

                if (!string.IsNullOrEmpty(_user.Avatar))
                {
                    imgAvatar.Source = _user.Avatar;
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", "Không thể tải thông tin", "OK");
        }

            }

            private async void OnEditClicked(object sender, EventArgs e)
            {
                await Shell.Current.GoToAsync(nameof(EditProfilePage));
            }

            private async void OnLogoutClicked(object sender, EventArgs e)
            {
                bool confirm = await DisplayAlert("Xác nhận", "Bạn có chắc chắn muốn đăng xuất?", "Đăng xuất", "Hủy");
                if (confirm)
                {
                    Preferences.Default.Remove("jwt_token");
                    await Shell.Current.GoToAsync("//LoginPage");
                }
            }
        }