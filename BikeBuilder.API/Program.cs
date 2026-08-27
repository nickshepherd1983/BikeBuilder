using Azure.Core;
using Azure.Core.Pipeline;
using Azure.Messaging.ServiceBus;
using Azure.Security.KeyVault.Secrets;
using Azure.Storage.Blobs;
using BikeBuilder.API.Endpoints;
using BikeBuilder.API.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();

var vaultUri = builder.Configuration["KeyVault:VaultUri"]
    ?? throw new InvalidOperationException("KeyVault:VaultUri is not configured.");

// The Key Vault Emulator's self-signed cert is only ever issued for "localhost", but this app
// reaches it via a Docker network alias - accept the emulator's cert regardless of the hostname
// used to reach it. Safe here because this always targets the local emulator, never a real vault.
var secretClient = new SecretClient(new Uri(vaultUri), new EmulatorTokenCredential(vaultUri), new SecretClientOptions
{
    DisableChallengeResourceVerification = true,
    Transport = new HttpClientTransport(new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    })
});

async Task<string> GetSecretAsync(string name) => (await secretClient.GetSecretAsync(name)).Value.Value;

builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
{
    ["ConnectionStrings:BikeBuilderDb"] = await GetSecretAsync("ConnectionStrings--BikeBuilderDb"),
    ["ConnectionStrings:BlobStorage"] = await GetSecretAsync("ConnectionStrings--BlobStorage"),
    ["ConnectionStrings:ServiceBus"] = await GetSecretAsync("ConnectionStrings--ServiceBus")
});

builder.Services.AddDbContext<BikeBuilderDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("BikeBuilderDb")));

builder.Services.AddSingleton(_ => new BlobServiceClient(builder.Configuration.GetConnectionString("BlobStorage")));
builder.Services.AddSingleton(sp => sp.GetRequiredService<BlobServiceClient>().GetBlobContainerClient("component-images"));
builder.Services.AddSingleton<ComponentImageStorageService>();

builder.Services.AddSingleton(_ => new ServiceBusClient(builder.Configuration.GetConnectionString("ServiceBus")));
builder.Services.AddSingleton(sp => sp.GetRequiredService<ServiceBusClient>().CreateSender(ServiceBusQueueNames.Notifications));
builder.Services.AddSingleton<IEventPublisher, ServiceBusEventPublisher>();

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

// Mirrors AzureKeyVaultEmulator.Client's own (now-obsolete) EmulatedTokenCredential - fetches a
// bearer token from the emulator's /token endpoint - but with a cert-trusting HttpClient, since
// that type's internal HttpClient can't be configured and fails TLS against a non-"localhost" host.
sealed class EmulatorTokenCredential(string vaultUri) : TokenCredential
{
    private static readonly HttpClient Client = new(new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    });

    public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken) =>
        GetTokenAsync(requestContext, cancellationToken).AsTask().GetAwaiter().GetResult();

    public override async ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        var response = await Client.GetAsync($"{vaultUri}/token", cancellationToken);
        response.EnsureSuccessStatusCode();
        var token = await response.Content.ReadAsStringAsync(cancellationToken);
        return new AccessToken(token, DateTimeOffset.UtcNow.AddDays(1));
    }
}
