using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.SignalR.Client;

namespace BikeBuilder.Web.Public.Components.Pages;

public partial class Home(ISnackbar _snackbar, IServer _server) : IAsyncDisposable
{
  HubConnection? _hubConnection;

  protected override async Task OnInitializedAsync()
  {
    // Even the "real" interactive circuit's component code still runs server-side (Blazor
    // Server has no client-side C#), so connecting during static prerendering isn't the only
    // concern here - both passes would otherwise try to self-connect using the externally
    // visible host:port from the original request (e.g. a Docker host-port mapping or a
    // reverse proxy's public address), which this process can't reach from inside itself.
    // Skip prerendering (it's discarded immediately anyway) and, once interactive, connect
    // using the address Kestrel is actually bound to rather than the request-derived one.
    if (!RendererInfo.IsInteractive)
      return;

    _hubConnection = new HubConnectionBuilder()
        .WithUrl(GetHubUri())
        .WithAutomaticReconnect()
        .Build();

    _hubConnection.On<string>("ReceiveNotification",
        message => InvokeAsync(() => _snackbar.Add(message, Severity.Info)));

    await _hubConnection.StartAsync();
  }

  Uri GetHubUri()
  {
    var address = _server.Features.Get<IServerAddressesFeature>()?.Addresses.FirstOrDefault()
        ?? throw new InvalidOperationException("Could not determine the server's own listening address.");

    var normalized = address
        .Replace("://+:", "://localhost:")
        .Replace("://*:", "://localhost:")
        .Replace("://[::]:", "://localhost:")
        .Replace("://0.0.0.0:", "://localhost:");

    return new Uri(new Uri(normalized), "/hubs/notifications");
  }

  public async ValueTask DisposeAsync()
  {
    if (_hubConnection is not null)
      await _hubConnection.DisposeAsync();
  }
}
