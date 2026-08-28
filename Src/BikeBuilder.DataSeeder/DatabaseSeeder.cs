using System.Text.Json;
using Microsoft.Azure.Cosmos;

namespace BikeBuilder.DataSeeder;

public sealed record SeedSummary(int Components, int BikeBuilds, int Ratings);

/// <summary>
/// The seeding core, callable both from this console app and from the integration-test
/// fixture (which seeds its own throwaway SQL + Cosmos emulator before tests run).
/// </summary>
public static class DatabaseSeeder
{
  public const string CosmosDatabaseId = "bikebuilder";
  public const string CosmosContainerId = "ratings";
  public const string CosmosPartitionKeyPath = "/bikeBuildId";

  // Same client options as BikeBuilder.API.Ratings/Program.cs so the stored JSON matches
  // what ListRatings/GetRatingSummaries read back (camelCase, /bikeBuildId partition key).
  public static CosmosClient CreateEmulatorCosmosClient(string connectionString) => new(connectionString, new CosmosClientOptions
  {
    ConnectionMode = ConnectionMode.Gateway,
    LimitToEndpoint = true,
    UseSystemTextJsonSerializerWithOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web),
    HttpClientFactory = () => new HttpClient(new HttpClientHandler
    {
      ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    })
  });

  public static async Task<Container> EnsureRatingsContainerAsync(CosmosClient cosmos)
  {
    var database = (await cosmos.CreateDatabaseIfNotExistsAsync(CosmosDatabaseId)).Database;
    return (await database.CreateContainerIfNotExistsAsync(CosmosContainerId, CosmosPartitionKeyPath)).Container;
  }

  public static async Task<SeedSummary> SeedAsync(BikeBuilderDbContext db, Container ratingsContainer, Random random)
  {
    var componentSeeds = ComponentCatalog.Generate(random, minimum: 1000);
    var components = componentSeeds.Select((seed, index) => new Component
    {
      Name = seed.Name,
      Cost = seed.Cost,
      Description = ComponentCatalog.Describe(seed),
      Sku = $"{seed.Brand[..Math.Min(3, seed.Brand.Length)].ToUpperInvariant()}-{index + 1:D4}",
      Manufacturer = seed.Manufacturer,
      Information = ComponentInformationSeeder.Create(seed, random)
    }).ToList();

    db.Components.AddRange(components);
    await db.SaveChangesAsync();

    var catalog = componentSeeds.Zip(components).ToList();
    var builds = new List<BikeBuild>();

    for (var i = 0; i < SeedPools.BuildNames.Length; i++)
    {
      var date = DateTimeOffset.UtcNow.AddDays(-random.Next(1, 365));
      var build = new BikeBuild
      {
        Name = SeedPools.BuildNames[i],
        Date = date,
        Description = SeedPools.BuildDescriptions[i % SeedPools.BuildDescriptions.Length]
      };

      // Walk a shuffled catalog until the target count is reached, skipping picks that
      // would push a kind past its recommended per-build maximum (2 tires, 1 fork, ...).
      var targetCount = random.Next(6, 13);
      var shuffled = Enumerable.Range(0, catalog.Count).OrderBy(_ => random.Next());
      var kindTotals = new Dictionary<Type, int>();
      foreach (var pick in shuffled)
      {
        if (build.BikeBuildComponents.Count >= targetCount)
          break;

        var (seed, component) = catalog[pick];
        var quantity = seed.Category is "Tire" or "Rim" ? 2 : 1;

        var kind = component.Information?.GetType();
        var recommendedMax = component.Information?.GetRecommendedMaxPerBuild();
        if (kind is not null && recommendedMax is not null)
        {
          var current = kindTotals.GetValueOrDefault(kind);
          if (current + quantity > recommendedMax)
            continue;

          kindTotals[kind] = current + quantity;
        }

        build.BikeBuildComponents.Add(new BikeBuildComponent
        {
          Component = component,
          Quantity = quantity,
          Date = date
        });
      }

      builds.Add(build);
    }

    db.BikeBuilds.AddRange(builds);
    await db.SaveChangesAsync();

    var documents = new List<RatingDocument>();
    foreach (var build in builds)
    {
      var ratingCount = random.Next(1, 31);
      for (var i = 0; i < ratingCount; i++)
      {
        var raterIndex = random.Next(SeedPools.RaterNames.Length);
        documents.Add(new RatingDocument
        {
          Id = Guid.NewGuid().ToString(),
          BikeBuildId = build.Id.ToString(),
          Stars = SeedPools.WeightedStars(random),
          Comment = random.NextDouble() < 0.8 ? SeedPools.Comments[random.Next(SeedPools.Comments.Length)] : null,
          UserId = $"auth0|seed-user-{raterIndex:D2}",
          UserName = SeedPools.RaterNames[raterIndex],
          CreatedAt = DateTimeOffset.UtcNow.AddDays(-random.Next(0, 180)).AddMinutes(-random.Next(0, 1440))
        });
      }
    }

    // Capped parallelism: ~1500 sequential emulator round-trips would take minutes.
    var throttle = new SemaphoreSlim(16);
    await Task.WhenAll(documents.Select(async document =>
    {
      await throttle.WaitAsync();
      try
      {
        await ratingsContainer.CreateItemAsync(document, new PartitionKey(document.BikeBuildId));
      }
      finally
      {
        throttle.Release();
      }
    }));

    return new SeedSummary(components.Count, builds.Count, documents.Count);
  }
}

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
