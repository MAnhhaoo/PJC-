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

        var success = await _userService.UpdateMeAsync(_user);

        if (success)
            await DisplayAlert("Thành công", "Đã cập nhật", "OK");
        else
            await DisplayAlert("Lỗi", "Không thể cập nhật", "OK");
    }
}