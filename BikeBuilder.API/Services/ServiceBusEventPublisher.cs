using System.Text.Json;
using Azure.Messaging.ServiceBus;

namespace BikeBuilder.API.Services;

public class ServiceBusEventPublisher(ServiceBusSender sender) : IEventPublisher
{
    public async Task PublishAsync<TEvent>(string messageType, TEvent payload, CancellationToken cancellationToken) where TEvent : class
    {
        var message = new ServiceBusMessage(JsonSerializer.SerializeToUtf8Bytes(payload))
        {
            ContentType = "application/json"
        };
        message.ApplicationProperties["MessageType"] = messageType;

        await sender.SendMessageAsync(message, cancellationToken);
    }
}
