using Azure.Core;
using Azure.Core.Pipeline;
using Azure.Security.KeyVault.Secrets;
using BikeBuilder.Web.Public;
using BikeBuilder.Web.Public.Components;
using BikeBuilder.Web.Public.Services;
using MudBlazor.Services;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

// Azure SDK messaging tracing (the ServiceBusProcessor.ProcessMessage span that continues
// the API's trace into this app) is still behind this experimental switch. Must be set
// before any ServiceBusClient is constructed.
AppContext.SetSwitch("Azure.Experimental.EnableActivitySource", true);

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();
builder.Services.AddSignalR();

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
var serviceBusConnectionString = (await secretClient.GetSecretAsync("ConnectionStrings--ServiceBus")).Value.Value;

builder.Services.AddSingleton(_ => new ServiceBusClient(serviceBusConnectionString));
builder.Services.AddHostedService<ServiceBusListenerBackgroundService>();

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("bikebuilder-web-public"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSource("Azure.*")                             // ServiceBusProcessor.ProcessMessage
        .AddSource("BikeBuilder.Web.Public")              // custom broadcast span in the listener
        .AddSource("Microsoft.AspNetCore.SignalR.Server") // client-invoked hub methods, if any appear
        .AddOtlpExporter(options =>
        {
          // The standard OTEL_EXPORTER_OTLP_ENDPOINT env var and its http://localhost:4317
          // default are honored automatically; this key is an optional appsettings override.
          var endpoint = builder.Configuration["Otel:OtlpEndpoint"];
          if (endpoint is not null)
            options.Endpoint = new Uri(endpoint);
        }));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
  app.UseExceptionHandler("/Error", createScopeForErrors: true);
  // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
  app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapHub<NotificationHub>("/hubs/notifications");

await app.RunAsync();

namespace BikeBuilder.Web.Public
{
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
}
