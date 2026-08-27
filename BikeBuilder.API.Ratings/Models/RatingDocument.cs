namespace BikeBuilder.API.Ratings.Models;

// Serialized to Cosmos with web defaults (camelCase), so Id becomes the "id" Cosmos requires
// and BikeBuildId matches the container's /bikeBuildId partition key path.
public sealed record RatingDocument
{
  public required string Id { get; init; }
  public required string BikeBuildId { get; init; }
  public required int Stars { get; init; }
  public string? Comment { get; init; }
  public required string UserId { get; init; }
  public required string UserName { get; init; }
  public required DateTimeOffset CreatedAt { get; init; }
}
