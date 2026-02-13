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

    // ===== GET (có JWT) =====
    public async Task<HttpResponseMessage> GetAsync(string url)
    {
        var token = await _local.GetItemAsync<string>("token");

        if (!string.IsNullOrEmpty(token))
        {
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        return await _http.GetAsync(url);
    }

    // ===== GET JSON (dùng cho Profile) =====
    public async Task<T?> GetFromJsonAsync<T>(string url)
    {
        var res = await GetAsync(url);
        return await res.Content.ReadFromJsonAsync<T>();
    }

    // ===== POST (LOGIN) =====
    public async Task<HttpResponseMessage> PostAsync<T>(string url, T data)
    {
        return await _http.PostAsJsonAsync(url, data);
    }

    // ===== PUT (UPDATE PROFILE) =====
    public async Task<HttpResponseMessage> PutAsync<T>(string url, T data)
    {
        var token = await _local.GetItemAsync<string>("token");

        if (!string.IsNullOrEmpty(token))
        {
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        return await _http.PutAsJsonAsync(url, data);
    }
}
