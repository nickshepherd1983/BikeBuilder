using Azure.Storage.Blobs;
using BikeBuilder.API.Data;
using BikeBuilder.API.Endpoints;
using BikeBuilder.API.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();

builder.Services.AddDbContext<BikeBuilderDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("BikeBuilderDb")));

builder.Services.AddSingleton(_ => new BlobServiceClient(builder.Configuration.GetConnectionString("BlobStorage")));
builder.Services.AddSingleton(sp => sp.GetRequiredService<BlobServiceClient>().GetBlobContainerClient("component-images"));
builder.Services.AddSingleton<ComponentImageStorageService>();

var webAppOrigins = builder.Configuration.GetSection("WebAppOrigins").Get<string[]>()
    ?? ["https://localhost:7200", "http://localhost:7201"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("BlazorWasmClient", policy =>
        policy.WithOrigins(webAppOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .WithExposedHeaders("Grpc-Status", "Grpc-Message", "Grpc-Encoding", "Grpc-Accept-Encoding"));
});

var app = builder.Build();

await app.Services.GetRequiredService<BlobContainerClient>().CreateIfNotExistsAsync();

app.UseCors("BlazorWasmClient");
app.UseGrpcWeb(new GrpcWebOptions { DefaultEnabled = true });

app.MapGrpcService<ComponentGrpcService>();
app.MapGrpcService<BikeBuildGrpcService>();
app.MapComponentImageEndpoints();
app.MapGet("/", () => "BikeBuilder.API gRPC endpoints — use a gRPC-Web client.");

app.Run();
