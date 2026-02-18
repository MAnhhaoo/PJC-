//using System.Net.Http.Headers;
//using System.Net.Http.Json;
//using Blazored.LocalStorage;

//namespace BlazorApp1.Services;

//public class ApiService
//{
//    private readonly HttpClient _http;
//    private readonly ILocalStorageService _local;

//    public ApiService(HttpClient http, ILocalStorageService local)
//    {
//        _http = http;
//        _local = local;
//    }

//    // ===== GET (có JWT) =====
//    public async Task<HttpResponseMessage> GetAsync(string url)
//    {
//        var token = await _local.GetItemAsync<string>("token");

//        if (!string.IsNullOrEmpty(token))
//        {
//            _http.DefaultRequestHeaders.Authorization =
//                new AuthenticationHeaderValue("Bearer", token);
//        }

//        return await _http.GetAsync(url);
//    }

//    // ===== GET JSON (dùng cho Profile) =====
//    public async Task<T?> GetFromJsonAsync<T>(string url)
//    {
//        var res = await GetAsync(url);
//        return await res.Content.ReadFromJsonAsync<T>();
//    }

//    // ===== POST (LOGIN) =====
//    public async Task<HttpResponseMessage> PostAsync<T>(string url, T data)
//    {
//        return await _http.PostAsJsonAsync(url, data);
//    }

//    // ===== PUT (UPDATE PROFILE) =====
//    public async Task<HttpResponseMessage> PutAsync<T>(string url, T data)
//    {
//        var token = await _local.GetItemAsync<string>("token");

//        if (!string.IsNullOrEmpty(token))
//        {
//            _http.DefaultRequestHeaders.Authorization =
//                new AuthenticationHeaderValue("Bearer", token);
//        }

//        return await _http.PutAsJsonAsync(url, data);
//    }
//}



using System.Net.Http.Headers;
using System.Net.Http.Json;
using Blazored.LocalStorage;

namespace BlazorApp1.Services;

public class ApiService
{
    private readonly HttpClient _http;
    private readonly ILocalStorageService _local;

    public ApiService(HttpClient http, ILocalStorageService local)
    {
        _http = http;
        _local = local;
    }

    // 🔥 Hàm gắn token chung
    private async Task AttachToken()
    {
        var token = await _local.GetItemAsync<string>("token"); // ⚠ đảm bảo key này đúng với lúc login

        if (!string.IsNullOrEmpty(token))
        {
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }
        else
        {
            _http.DefaultRequestHeaders.Authorization = null;
        }
    }

    // ===== GET =====
    public async Task<T?> GetFromJsonAsync<T>(string url)
    {
        await AttachToken();

        var response = await _http.GetAsync(url);

        if (!response.IsSuccessStatusCode)
            return default;

        return await response.Content.ReadFromJsonAsync<T>();
    }

    // ===== POST =====
    public async Task<HttpResponseMessage> PostAsync<T>(string url, T data)
    {
        await AttachToken();
        return await _http.PostAsJsonAsync(url, data);
    }


    // ===== PUT =====
    public async Task<HttpResponseMessage> PutAsync<T>(string url, T data)
    {
        await AttachToken();
        return await _http.PutAsJsonAsync(url, data);
    }



    // ===== LOGIN =====
    public async Task<bool> LoginAsync<T>(string url, T data)
    {
        var response = await _http.PostAsJsonAsync(url, data);

        if (!response.IsSuccessStatusCode)
            return false;

        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();

        if (result != null && !string.IsNullOrEmpty(result.Token))
        {
            await _local.SetItemAsync("token", result.Token); // 🔥 LƯU Ở ĐÂY
        }

        return true;
    }

    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
    }

}
