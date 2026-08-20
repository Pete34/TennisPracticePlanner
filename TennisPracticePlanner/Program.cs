using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using TennisPracticePlanner;
using TennisPracticePlanner.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<TennisPracticeDataService>();
builder.Services.AddScoped<CloudTennisPracticeDataService>();
builder.Services.AddScoped<ITennisPracticeDataService, CompositeTennisPracticeDataService>();
builder.Services.AddScoped(sp => (CompositeTennisPracticeDataService)sp.GetRequiredService<ITennisPracticeDataService>());

await builder.Build().RunAsync();
