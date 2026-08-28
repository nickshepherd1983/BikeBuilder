using Azure.Core;
using Azure.Core.Pipeline;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

// Azure SDK messaging tracing (Service Bus send spans + traceparent stamping on the
// RatingCreated events) is still behind this experimental switch. Must be set before any
// ServiceBusClient is constructed.
AppContext.SetSwitch("Azure.Experimental.EnableActivitySource", true);

var builder = FunctionsApplication.CreateBuilder(args);
builder.ConfigureFunctionsWebApplication();

builder.Services.AddOpenTelemetry()
    .UseFunctionsWorkerDefaults() // correlates worker spans with the Functions host's invocation spans
    .ConfigureResource(resource => resource.AddService("bikebuilder-ratings"))
    .WithTracing(tracing => tracing
        // Deliberately no AddAspNetCoreInstrumentation: the Functions host already emits the
        // request/invocation span and adding it in the worker double-reports every request.
        .AddHttpClientInstrumentation() // Cosmos gateway HTTP + JWKS fetches
        .AddSource("Azure.*")           // Azure.Cosmos.Operation + Service Bus send
        .AddOtlpExporter(options =>
        {
          // The standard OTEL_EXPORTER_OTLP_ENDPOINT env var and its http://localhost:4317
          // default are honored automatically; this key is an optional appsettings override.
          var endpoint = builder.Configuration["Otel:OtlpEndpoint"];
          if (endpoint is not null)
            options.Endpoint = new Uri(endpoint);
        }));

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
      // Explicit opt-in: emits "Azure.Cosmos.Operation" activities for container operations
      // (the default has flip-flopped between SDK builds, so be deterministic).
      CosmosClientTelemetryOptions = new CosmosClientTelemetryOptions { DisableDistributedTracing = false },
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
