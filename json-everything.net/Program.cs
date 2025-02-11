using JsonEverythingNet;
using JsonEverythingNet.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddSingleton<CookieManager>();
builder.Services.AddScoped<ApiDocGenerationService>();

var host = builder.Build();
var client = host.Services.GetService<HttpClient>();

await AnchorRegistry.RegisterDocs(client!);

await host.RunAsync();