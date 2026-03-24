using System.Net.Http.Headers;
using Microsoft.Maui.Storage;

namespace TourismApp.Services;

public class AuthHeaderHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Tự động lấy token từ bộ nhớ máy
        var token = await SecureStorage.GetAsync("auth_token");

        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}