namespace BikeBuilder.Contracts.Events;

public sealed record RatingCreatedEvent
{
  public required string RatingId { get; init; }
  public required string BikeBuildId { get; init; }
  public required string BikeBuildName { get; init; }
  public required int Stars { get; init; }
  public required string UserName { get; init; }
  public required DateTimeOffset CreatedAt { get; init; }
}
