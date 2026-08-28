using Azure.Core;
using Azure.Core.Pipeline;
using Azure.Security.KeyVault.Secrets;
using BikeBuilder.DataSeeder;
using Microsoft.Azure.Cosmos;

// Seeds the local dev stack (docker compose up -d first) with 1000+ real-sounding
// components, 100 bike builds, and 1-30 Cosmos ratings per build.
// Refuses to run against a database that already has components; pass --reset to wipe
// components, bike builds, and ratings first.

var reset = args.Contains("--reset");
var vaultUri = Environment.GetEnvironmentVariable("KeyVault__VaultUri") ?? "https://localhost:4997";

Console.WriteLine($"Fetching connection strings from the Key Vault emulator at {vaultUri}");

// Same trust-the-emulator setup as BikeBuilder.API/Program.cs.
var secretClient = new SecretClient(new Uri(vaultUri), new EmulatorTokenCredential(vaultUri), new SecretClientOptions
{
  DisableChallengeResourceVerification = true,
  Transport = new HttpClientTransport(new HttpClientHandler
  {
    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
  })
});

async Task<string> GetSecretAsync(string name) => (await secretClient.GetSecretAsync(name)).Value.Value;

var sqlConnectionString = await GetSecretAsync("ConnectionStrings--BikeBuilderDb");
var cosmosConnectionString = await GetSecretAsync("ConnectionStrings--Cosmos");

var dbOptions = new DbContextOptionsBuilder<BikeBuilderDbContext>().UseSqlServer(sqlConnectionString).Options;
await using var db = new BikeBuilderDbContext(dbOptions);
await db.Database.MigrateAsync();

using var cosmos = DatabaseSeeder.CreateEmulatorCosmosClient(cosmosConnectionString);
var ratingsContainer = await DatabaseSeeder.EnsureRatingsContainerAsync(cosmos);

if (reset)
{
  Console.WriteLine("--reset: deleting existing bike builds, components, and ratings...");
  await db.BikeBuildComponents.ExecuteDeleteAsync();
  await db.BikeBuilds.ExecuteDeleteAsync();
  await db.ComponentImages.ExecuteDeleteAsync();
  await db.Components.ExecuteDeleteAsync();
  await ratingsContainer.DeleteContainerAsync();
  ratingsContainer = await DatabaseSeeder.EnsureRatingsContainerAsync(cosmos);
}
else if (await db.Components.AnyAsync())
{
  Console.WriteLine("The database already contains components - run again with --reset to wipe components, bike builds, and ratings before seeding.");
  return 1;
}

var summary = await DatabaseSeeder.SeedAsync(db, ratingsContainer, new Random(20260827));

Console.WriteLine($"Seeded {summary.Components} components.");
Console.WriteLine($"Seeded {summary.BikeBuilds} bike builds and {summary.Ratings} ratings.");
Console.WriteLine("Ratings were written straight to Cosmos, so no Service Bus notifications were published.");
return 0;

namespace BikeBuilder.DataSeeder
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
