namespace BikeBuilder.Contracts.Events;

public sealed record ComponentCreatedEvent
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required decimal Cost { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}
