namespace BikeBuilder.API.Services;

public interface IEventPublisher
{
    Task PublishAsync<TEvent>(string messageType, TEvent payload, CancellationToken cancellationToken) where TEvent : class;
}
