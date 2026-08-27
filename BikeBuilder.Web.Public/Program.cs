using Azure.Core;
using Azure.Core.Pipeline;
using Azure.Security.KeyVault.Secrets;
using BikeBuilder.Web.Public.Components;
using BikeBuilder.Web.Public.Services;
using MudBlazor.Services;

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
