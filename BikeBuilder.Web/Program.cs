using BikeBuilder.API.Protos;
using BikeBuilder.Web;
using BikeBuilder.Web.Services;
using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddMudServices();

builder.Services.AddOidcAuthentication(options =>
{
  options.ProviderOptions.Authority = builder.Configuration["Auth0:Authority"];
  options.ProviderOptions.ClientId = builder.Configuration["Auth0:ClientId"];
  // Auth0 SPAs only support authorization code + PKCE; the library defaults to implicit flow.
  options.ProviderOptions.ResponseType = "code";
  // The audience parameter is what makes Auth0 issue a JWT access token instead of an opaque
  // one; other issuers (like the integration tests' stub) simply ignore it.
  options.ProviderOptions.AdditionalProviderParameters.Add("audience", builder.Configuration["Auth0:Audience"]!);
  // Empty against real Auth0; the tests' IdentityServer-based stub mints the aud claim from
  // API scopes rather than the audience parameter, so it needs the API scope requested here.
  foreach (var scope in (builder.Configuration["Auth0:ExtraScopes"] ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries))
  {
    options.ProviderOptions.DefaultScopes.Add(scope);
  }
});

var apiBaseAddress = builder.Configuration["ApiBaseAddress"] ?? "https://localhost:7100";

// Scoped rather than singleton so the factory can resolve the scoped IAccessTokenProvider;
// in WASM there is only the root scope, so this is still effectively one channel.
builder.Services.AddScoped(sp =>
{
  var tokenProvider = sp.GetRequiredService<IAccessTokenProvider>();
  var callCredentials = CallCredentials.FromInterceptor(async (context, metadata) =>
  {
    var result = await tokenProvider.RequestAccessToken();
    if (result.TryGetToken(out var token))
    {
      metadata.Add("Authorization", $"Bearer {token.Value}");
    }
    // No token: the call goes out unauthenticated and the API answers with
    // RpcException(Unauthenticated). Protected pages sit behind AuthorizeRouteView,
    // so this only happens in edge races around login/logout.
  });

  var apiUri = new Uri(apiBaseAddress);
  var isHttps = apiUri.Scheme == Uri.UriSchemeHttps;
  return GrpcChannel.ForAddress(apiUri, new GrpcChannelOptions
  {
    HttpHandler = new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()),
    Credentials = isHttps
        ? ChannelCredentials.Create(new SslCredentials(), callCredentials)
        : ChannelCredentials.Create(ChannelCredentials.Insecure, callCredentials),
    // Required to send bearer tokens over the http:// address the Testcontainers
    // environment uses; the https dev/prod path never sets it.
    UnsafeUseInsecureChannelCallCredentials = !isHttps
  });
});

builder.Services.AddScoped(sp => new ComponentService.ComponentServiceClient(sp.GetRequiredService<GrpcChannel>()));
builder.Services.AddScoped(sp => new BikeBuildService.BikeBuildServiceClient(sp.GetRequiredService<GrpcChannel>()));

builder.Services.AddScoped(sp =>
{
  var handler = sp.GetRequiredService<AuthorizationMessageHandler>()
      .ConfigureHandler(authorizedUrls: [apiBaseAddress]);
  handler.InnerHandler = new HttpClientHandler();
  return new ComponentImageClient(new HttpClient(handler) { BaseAddress = new Uri(apiBaseAddress) });
});

var ratingsApiBaseAddress = builder.Configuration["RatingsApiBaseAddress"] ?? "http://localhost:7071";

builder.Services.AddScoped(sp =>
{
  var handler = sp.GetRequiredService<AuthorizationMessageHandler>()
      .ConfigureHandler(authorizedUrls: [ratingsApiBaseAddress]);
  handler.InnerHandler = new HttpClientHandler();
  return new RatingsClient(new HttpClient(handler) { BaseAddress = new Uri(ratingsApiBaseAddress) });
});

await builder.Build().RunAsync();
