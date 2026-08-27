using System.Text.Json;
using Azure.Messaging.ServiceBus;

namespace BikeBuilder.API.Ratings.Services;

// Mirrors BikeBuilder.API's ServiceBusEventPublisher exactly: PascalCase payload (default
// JsonSerializer options) and the MessageType application property BikeBuilder.Web.Public's
// listener switches on. Kept as a copy rather than shared via Contracts so Contracts stays
// free of Azure package references.
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
