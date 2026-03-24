using TourismApp.Models;
using TourismApp.Services;

namespace TourismApp;

public partial class EditProfilePage : ContentPage
{
    private readonly UserService _userService;
    private User _user;

    public EditProfilePage(UserService userService)
    {
        InitializeComponent();
        _userService = userService;
    }
    private async void OnAvatarTapped(object sender, EventArgs e)
    {
        try
        {
            var result = await MediaPicker.Default.PickPhotoAsync();
            if (result != null)
            {
                // 1. Hiển thị tạm thời lên UI
                var localStream = await result.OpenReadAsync();
                imgAvatar.Source = ImageSource.FromStream(() => localStream);

                // 2. Upload lên Server
                var newUrl = await _userService.UploadAvatarAsync(result);
                if (!string.IsNullOrEmpty(newUrl))
                {
                    _user.Avatar = newUrl;
                    await DisplayAlert("Thông báo", "Đã tải ảnh lên thành công", "OK");
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", "Không thể chọn ảnh: " + ex.Message, "OK");
        }
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            _user = await _userService.GetMeAsync();

            if (_user == null)
            {
                await DisplayAlert("Lỗi", "Không tải được dữ liệu", "OK");
                return;
            }

            txtEmail.Text = _user.Email;
            txtFullName.Text = _user.FullName;
            txtPhone.Text = _user.Phone;
            txtAddress.Text = _user.Address;

            // Hiển thị avatar nếu có
            if (!string.IsNullOrEmpty(_user.Avatar))
            {
                imgAvatar.Source = _user.Avatar;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", ex.Message, "OK");
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        if (_user == null) return;

        // Không cho phép xóa dữ liệu đã tồn tại
        if (!string.IsNullOrEmpty(_user.FullName) &&
            string.IsNullOrWhiteSpace(txtFullName.Text))
        {
            await DisplayAlert("Lỗi", "Không được xóa Họ tên", "OK");
            return;
        }

        if (!string.IsNullOrEmpty(_user.Phone) &&
            string.IsNullOrWhiteSpace(txtPhone.Text))
        {
            await DisplayAlert("Lỗi", "Không được xóa Số điện thoại", "OK");
            return;
        }

        if (!string.IsNullOrEmpty(_user.Address) &&
            string.IsNullOrWhiteSpace(txtAddress.Text))
        {
            await DisplayAlert("Lỗi", "Không được xóa Địa chỉ", "OK");
            return;
        }

        // Nếu hợp lệ thì update
        _user.FullName = txtFullName.Text;
        _user.Phone = txtPhone.Text;
        _user.Address = txtAddress.Text;


        try
        {
            var success = await _userService.UpdateMeAsync(_user);

            if (success)
            {
                // Sử dụng Dispatcher để đảm bảo chạy trên luồng giao diện chính
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await DisplayAlert("Thành công", "Đã cập nhật hồ sơ", "OK");
                    await Shell.Current.GoToAsync("//CustomerHomePage/ProfilePage");
                });
            }
            else
            {
                await DisplayAlert("Lỗi", "Không thể cập nhật thông tin lên Server", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi hệ thống", ex.Message, "OK");
        }
    }
}