//using BlazorApp1;
//using BlazorApp1.Services;   // 🔥 PHẢI CÓ
//using Blazored.LocalStorage;
//using Microsoft.AspNetCore.Components.Web;
//using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
//using MudBlazor.Services;

//var builder = WebAssemblyHostBuilder.CreateDefault(args);

//builder.RootComponents.Add<App>("#app");
//builder.RootComponents.Add<HeadOutlet>("head::after");

//builder.Services.AddBlazoredLocalStorage();
//builder.Services.AddMudServices();

//builder.Services.AddScoped(sp =>
//{
//    var client = new HttpClient
//    {
//        BaseAddress = new Uri("https://localhost:7111/")
//    };

//    return client;
//});


//builder.Services.AddScoped<ApiService>(); // 🔥

//await builder.Build().RunAsync();





using BlazorApp1;
using BlazorApp1.Services;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddBlazoredLocalStorage();
builder.Services.AddMudServices();

builder.Services.AddScoped<AuthMessageHandler>();

builder.Services.AddScoped(sp =>
{
    var handler = sp.GetRequiredService<AuthMessageHandler>();
    handler.InnerHandler = new HttpClientHandler();

    return new HttpClient(handler)
    {
        BaseAddress = new Uri("https://localhost:7111/")
    };
});

builder.Services.AddScoped<ApiService>();

await builder.Build().RunAsync();
