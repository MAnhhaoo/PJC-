using TourismApp.Services;
using TourismApp.Models;

namespace TourismApp;

public partial class ProfilePage : ContentPage
{
    private readonly UserService _userService;
    private readonly HttpClient _httpClient;
    private readonly LanguageService _lang;
    private User _user;

    public ProfilePage(UserService userService, HttpClient httpClient, LanguageService languageService)
    {
        InitializeComponent();
        _userService = userService;
        _httpClient = httpClient;
        _lang = languageService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        Title = _lang["ProfileTitle"];
        lblEmailLabel.Text = _lang["Email"];
        lblPhoneLabel.Text = _lang["Phone"];
        lblAddressLabel.Text = _lang["Address"];
        btnEditProfile.Text = _lang["EditProfile"];
        btnLogout.Text = _lang["Logout"];

        try
        {
            _user = await _userService.GetMeAsync();

            if (_user != null)
            {
                lblEmail.Text = _user.Email;
                lblFullNameHeader.Text = _user.FullName; // Hiển thị tên ở Header
                lblPhone.Text = string.IsNullOrEmpty(_user.Phone) ? _lang["NotUpdated"] : _user.Phone;
                lblAddress.Text = string.IsNullOrEmpty(_user.Address) ? _lang["NotUpdated"] : _user.Address;

                if (!string.IsNullOrEmpty(_user.Avatar))
                {
                    // Resolve avatar URL to use the correct server address
                    var avatarUrl = _user.Avatar;
                    if (avatarUrl.StartsWith("http"))
                    {
                        try
                        {
                            var uri = new Uri(avatarUrl);
                            avatarUrl = new Uri(_httpClient.BaseAddress!, uri.PathAndQuery).ToString();
                        }
                        catch { }
                    }
                    else
                    {
                        avatarUrl = new Uri(_httpClient.BaseAddress!, avatarUrl).ToString();
                    }
                    imgAvatar.Source = ImageSource.FromUri(new Uri(avatarUrl));
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert(_lang["Error"], _lang["ErrorLoadInfo"], _lang["OK"]);
        }

            }

            private async void OnEditClicked(object sender, EventArgs e)
            {
                await Shell.Current.GoToAsync(nameof(EditProfilePage));
            }

            private async void OnLogoutClicked(object sender, EventArgs e)
            {
                bool confirm = await DisplayAlert(_lang["LogoutConfirm"], _lang["LogoutConfirmMsg"], _lang["LogoutBtn"], _lang["Cancel"]);
                if (confirm)
                {
                    Preferences.Default.Remove("jwt_token");
                    await Shell.Current.GoToAsync("//LoginPage");
                }
            }
        }