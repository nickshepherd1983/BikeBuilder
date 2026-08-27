using System.Text.Json;
using Azure.Core;
using Azure.Core.Pipeline;
using Azure.Security.KeyVault.Secrets;
using BikeBuilder.DataSeeder;
using Microsoft.Azure.Cosmos;

// Seeds the local dev stack (docker compose up -d first) with 1000+ real-sounding
// components, 20 bike builds, and 1-30 Cosmos ratings per build.
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

// Same client options as BikeBuilder.API.Ratings/Program.cs so the stored JSON matches
// what ListRatings/GetRatingSummaries read back (camelCase, /bikeBuildId partition key).
using var cosmos = new CosmosClient(cosmosConnectionString, new CosmosClientOptions
{
  ConnectionMode = ConnectionMode.Gateway,
  LimitToEndpoint = true,
  UseSystemTextJsonSerializerWithOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web),
  HttpClientFactory = () => new HttpClient(new HttpClientHandler
  {
    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
  })
});

var cosmosDatabase = (await cosmos.CreateDatabaseIfNotExistsAsync("bikebuilder")).Database;
var ratingsContainer = (await cosmosDatabase.CreateContainerIfNotExistsAsync("ratings", "/bikeBuildId")).Container;

if (reset)
{
  Console.WriteLine("--reset: deleting existing bike builds, components, and ratings...");
  await db.BikeBuildComponents.ExecuteDeleteAsync();
  await db.BikeBuilds.ExecuteDeleteAsync();
  await db.ComponentImages.ExecuteDeleteAsync();
  await db.Components.ExecuteDeleteAsync();
  await ratingsContainer.DeleteContainerAsync();
  ratingsContainer = (await cosmosDatabase.CreateContainerIfNotExistsAsync("ratings", "/bikeBuildId")).Container;
}
else if (await db.Components.AnyAsync())
{
  Console.WriteLine("The database already contains components - run again with --reset to wipe components, bike builds, and ratings before seeding.");
  return 1;
}

var random = new Random(20260827);

var componentSeeds = ComponentCatalog.Generate(random, minimum: 1000);
var components = componentSeeds.Select((seed, index) => new Component
{
  Name = seed.Name,
  Cost = seed.Cost,
  Description = ComponentCatalog.Describe(seed),
  Sku = $"{seed.Brand[..Math.Min(3, seed.Brand.Length)].ToUpperInvariant()}-{index + 1:D4}",
  Manufacturer = seed.Manufacturer
}).ToList();

db.Components.AddRange(components);
await db.SaveChangesAsync();
Console.WriteLine($"Seeded {components.Count} components.");

var catalog = componentSeeds.Zip(components).ToList();
var builds = new List<BikeBuild>();

for (var i = 0; i < SeedPools.BuildNames.Length; i++)
{
  var date = DateTimeOffset.UtcNow.AddDays(-random.Next(1, 365));
  var build = new BikeBuild
  {
    Name = SeedPools.BuildNames[i],
    Date = date,
    Description = SeedPools.BuildDescriptions[i]
  };

  var picks = Enumerable.Range(0, catalog.Count).OrderBy(_ => random.Next()).Take(random.Next(6, 13));
  foreach (var pick in picks)
  {
    var (seed, component) = catalog[pick];
    build.BikeBuildComponents.Add(new BikeBuildComponent
    {
      Component = component,
      Quantity = seed.Category is "Tire" or "Rim" ? 2 : 1,
      Date = date
    });
  }

  builds.Add(build);
}

db.BikeBuilds.AddRange(builds);
await db.SaveChangesAsync();

var totalRatings = 0;
foreach (var build in builds)
{
  var ratingCount = random.Next(1, 31);
  for (var i = 0; i < ratingCount; i++)
  {
    var raterIndex = random.Next(SeedPools.RaterNames.Length);
    var document = new RatingDocument
    {
      Id = Guid.NewGuid().ToString(),
      BikeBuildId = build.Id.ToString(),
      Stars = SeedPools.WeightedStars(random),
      Comment = random.NextDouble() < 0.8 ? SeedPools.Comments[random.Next(SeedPools.Comments.Length)] : null,
      UserId = $"auth0|seed-user-{raterIndex:D2}",
      UserName = SeedPools.RaterNames[raterIndex],
      CreatedAt = DateTimeOffset.UtcNow.AddDays(-random.Next(0, 180)).AddMinutes(-random.Next(0, 1440))
    };

    await ratingsContainer.CreateItemAsync(document, new PartitionKey(document.BikeBuildId));
    totalRatings++;
  }
}

Console.WriteLine($"Seeded {builds.Count} bike builds and {totalRatings} ratings.");
Console.WriteLine("Ratings were written straight to Cosmos, so no Service Bus notifications were published.");
return 0;

// Mirrors BikeBuilder.API.Ratings/Models/RatingDocument.cs - written directly to Cosmos in
// the exact shape ListRatings and GetRatingSummaries read back.
sealed record RatingDocument
{
  public required string Id { get; init; }
  public required string BikeBuildId { get; init; }
  public required int Stars { get; init; }
  public string? Comment { get; init; }
  public required string UserId { get; init; }
  public required string UserName { get; init; }
  public required DateTimeOffset CreatedAt { get; init; }
}

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
