using Azure.Core;
using Azure.Core.Pipeline;
using Azure.Security.KeyVault.Secrets;
using BikeBuilder.API.Endpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;

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

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
      options.Authority = builder.Configuration["Auth0:Authority"]
          ?? throw new InvalidOperationException("Auth0:Authority is not configured.");
      options.Audience = builder.Configuration["Auth0:Audience"];
      // False only in the integration-test environment, where the stub OIDC issuer is plain http.
      options.RequireHttpsMetadata = builder.Configuration.GetValue("Auth0:RequireHttpsMetadata", true);
      options.TokenValidationParameters.NameClaimType = "sub";
    });
builder.Services.AddAuthorization();

var app = builder.Build();

await app.Services.GetRequiredService<BlobContainerClient>().CreateIfNotExistsAsync();

app.UseCors("BlazorWasmClient");
// gRPC-Web unwrapping must happen before authentication reads the request.
app.UseGrpcWeb(new GrpcWebOptions { DefaultEnabled = true });
app.UseAuthentication();
app.UseAuthorization();

app.MapGrpcService<ComponentGrpcService>().RequireAuthorization();
app.MapGrpcService<BikeBuildGrpcService>().RequireAuthorization();
app.MapComponentImageEndpoints();
// Stays anonymous - the integration-test fixture uses it as the container health probe.
app.MapGet("/", () => "BikeBuilder.API gRPC endpoints — use a gRPC-Web client.");

app.Run();

// Mirrors AzureKeyVaultEmulator.Client's own (now-obsolete) EmulatedTokenCredential - fetches a
// bearer token from the emulator's /token endpoint - but with a cert-trusting HttpClient, since
// that type's internal HttpClient can't be configured and fails TLS against a non-"localhost" host.
sealed class EmulatorTokenCredential(string vaultUri) : TokenCredential
{
  static readonly HttpClient Client = new(new HttpClientHandler
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
