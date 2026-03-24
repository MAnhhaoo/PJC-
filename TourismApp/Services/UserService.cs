using System.Net.Http.Headers;
using System.Net.Http.Json;
using TourismApp.Models;

namespace TourismApp.Services;

public class UserService
{
    private readonly HttpClient _httpClient;

    public UserService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    private async Task AddJwt()
    {
        var token = await SecureStorage.GetAsync("auth_token");
        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }
    }

    public async Task<User> GetMeAsync()
    {
        try
        {
            await AddJwt();
            return await _httpClient.GetFromJsonAsync<User>("api/users/me");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetMe Error: {ex.Message}");
            return null;
        }
    }

    // CHỈ GIỮ 1 HÀM UpdateMeAsync DUY NHẤT
    public async Task<bool> UpdateMeAsync(User user)
    {
        try
        {
            await AddJwt();

            // Khớp với UpdateProfileDto ở Backend (Lưu ý: Avatar cũng cần gửi lên nếu có)
            var updateData = new
            {
                fullName = user.FullName,
                phone = user.Phone,
                address = user.Address,
                avatar = user.Avatar,
                userLevel = user.UserLevel
            };

            var response = await _httpClient.PutAsJsonAsync("api/users/me", updateData);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"UpdateMe Error: {ex.Message}");
            return false;
        }
    }

    public async Task<string> UploadAvatarAsync(FileResult file)
    {
        try
        {
            await AddJwt();
            var content = new MultipartFormDataContent();
            var stream = await file.OpenReadAsync();

            // "file" phải khớp với tham số IFormFile file trong Backend
            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
            content.Add(fileContent, "file", file.FileName);

            var response = await _httpClient.PostAsync("api/users/upload-avatar", content);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<UploadResult>();
                return result?.Url;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"UploadAvatar Error: {ex.Message}");
        }
        return null;
    }

    public class UploadResult { public string Url { get; set; } }
}