using BikeBuilder.Contracts.Events;
using BikeBuilder.Contracts.Messaging;

namespace BikeBuilder.Web.Public.Services;

public class ServiceBusListenerBackgroundService(
    ServiceBusClient client,
    IHubContext<NotificationHub> hubContext,
    ILogger<ServiceBusListenerBackgroundService> logger) : BackgroundService
{
  private ServiceBusProcessor? _processor;

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    _processor = client.CreateProcessor(ServiceBusQueueNames.Notifications, new ServiceBusProcessorOptions());
    _processor.ProcessMessageAsync += OnMessageReceivedAsync;
    _processor.ProcessErrorAsync += args =>
    {
      logger.LogError(args.Exception, "Service Bus error while processing notifications");
      return Task.CompletedTask;
    };

    await _processor.StartProcessingAsync(stoppingToken);
  }

  private async Task OnMessageReceivedAsync(ProcessMessageEventArgs args)
  {
    var messageType = args.Message.ApplicationProperties.GetValueOrDefault("MessageType") as string;

    var text = messageType switch
    {
      ServiceBusMessageTypes.ComponentCreated =>
          $"New component added: {args.Message.Body.ToObjectFromJson<ComponentCreatedEvent>()!.Name}",
      ServiceBusMessageTypes.BikeBuildCreated =>
          $"New bike build created: {args.Message.Body.ToObjectFromJson<BikeBuildCreatedEvent>()!.Name}",
      ServiceBusMessageTypes.RatingCreated =>
          FormatRatingCreated(args.Message.Body.ToObjectFromJson<RatingCreatedEvent>()!),
      _ => null
    };

    if (text is not null)
    {
      await hubContext.Clients.All.SendAsync("ReceiveNotification", text, args.CancellationToken);
    }

    await args.CompleteMessageAsync(args.Message, args.CancellationToken);
  }

  private static string FormatRatingCreated(RatingCreatedEvent rating) =>
      $"New {rating.Stars}-star rating for {rating.BikeBuildName}";

  public override async Task StopAsync(CancellationToken cancellationToken)
  {
    if (_processor is not null)
    {
      await _processor.StopProcessingAsync(cancellationToken);
    }

    await base.StopAsync(cancellationToken);
  }
}
