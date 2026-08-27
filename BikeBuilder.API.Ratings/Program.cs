using Azure.Core;
using Azure.Core.Pipeline;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);
builder.ConfigureFunctionsWebApplication();

var vaultUri = builder.Configuration["KeyVault:VaultUri"]
    ?? throw new InvalidOperationException("KeyVault:VaultUri is not configured.");

// Same trust-the-emulator setup as BikeBuilder.API/Program.cs: the Key Vault Emulator's
// self-signed cert is only issued for "localhost" but may be reached via a Docker alias.
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
  ["ConnectionStrings:Cosmos"] = await GetSecretAsync("ConnectionStrings--Cosmos"),
  ["ConnectionStrings:ServiceBus"] = await GetSecretAsync("ConnectionStrings--ServiceBus")
});

builder.UseMiddleware<CorsMiddleware>();
builder.UseWhen<JwtAuthenticationMiddleware>(context => context.FunctionDefinition.Name == "CreateRating");

builder.Services.AddSingleton(_ => new CosmosClient(
    builder.Configuration.GetConnectionString("Cosmos"),
    new CosmosClientOptions
    {
      // Gateway + LimitToEndpoint: the emulator advertises "localhost" as its endpoint, so
      // the SDK must stick to the address we gave it when reached via a Docker alias.
      ConnectionMode = ConnectionMode.Gateway,
      LimitToEndpoint = true,
      // camelCase documents, so the stored JSON matches the REST API's shape ("id",
      // "bikeBuildId", ...) and the /bikeBuildId partition key path.
      UseSystemTextJsonSerializerWithOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web),
      // True only against the emulator's self-signed cert; never set in real Azure.
      HttpClientFactory = builder.Configuration.GetValue("Cosmos:DisableServerCertificateValidation", false)
          ? () => new HttpClient(new HttpClientHandler
          {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
          })
          : null
    }));
builder.Services.AddSingleton(sp => sp.GetRequiredService<CosmosClient>().GetContainer("bikebuilder", "ratings"));

builder.Services.AddSingleton(_ => new ServiceBusClient(builder.Configuration.GetConnectionString("ServiceBus")));
builder.Services.AddSingleton(sp => sp.GetRequiredService<ServiceBusClient>().CreateSender(ServiceBusQueueNames.Notifications));
builder.Services.AddSingleton<IEventPublisher, ServiceBusEventPublisher>();

var app = builder.Build();

await CosmosInitializer.EnsureCreatedAsync(app.Services.GetRequiredService<CosmosClient>(),
    databaseId: "bikebuilder", containerId: "ratings", partitionKeyPath: "/bikeBuildId",
    timeout: TimeSpan.FromSeconds(90));

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
