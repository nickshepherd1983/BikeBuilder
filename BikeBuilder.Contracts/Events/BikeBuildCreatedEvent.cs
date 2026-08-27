namespace BikeBuilder.Contracts.Events;

public sealed record BikeBuildCreatedEvent
{
  public required int Id { get; init; }
  public required string Name { get; init; }
  public required DateTimeOffset CreatedAt { get; init; }
}
