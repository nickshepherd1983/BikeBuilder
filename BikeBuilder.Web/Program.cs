using BikeBuilder.API.Protos;
using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using BikeBuilder.Web;
using BikeBuilder.Web.Services;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddMudServices();

var apiBaseAddress = builder.Configuration["ApiBaseAddress"] ?? "https://localhost:7100";

builder.Services.AddSingleton(_ =>
{
    var httpHandler = new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler());
    return GrpcChannel.ForAddress(apiBaseAddress, new GrpcChannelOptions { HttpHandler = httpHandler });
});

builder.Services.AddScoped(sp => new ComponentService.ComponentServiceClient(sp.GetRequiredService<GrpcChannel>()));
builder.Services.AddScoped(sp => new BikeBuildService.BikeBuildServiceClient(sp.GetRequiredService<GrpcChannel>()));

builder.Services.AddScoped(_ => new ComponentImageClient(new HttpClient { BaseAddress = new Uri(apiBaseAddress) }));

await builder.Build().RunAsync();
