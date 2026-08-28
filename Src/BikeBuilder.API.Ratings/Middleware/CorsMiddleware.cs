namespace BikeBuilder.API.Ratings.Middleware;

// The Functions host has no supported way to configure CORS via environment variables when
// containerized, so CORS lives in the worker: this middleware decorates every response
// (including 401s/400s - it runs before auth) and CorsPreflightFunction answers OPTIONS.
sealed class CorsMiddleware(IConfiguration configuration) : IFunctionsWorkerMiddleware
{
  readonly string[] _allowedOrigins = configuration.GetSection("WebAppOrigins").Get<string[]>()
      ?? ["https://localhost:7200", "http://localhost:7201"];

  public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
  {
    var http = context.GetHttpContext();
    if (http is not null)
    {
      var origin = http.Request.Headers.Origin.ToString();
      if (_allowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
      {
        http.Response.Headers.AccessControlAllowOrigin = origin;
        http.Response.Headers.Vary = "Origin";
        http.Response.Headers.AccessControlAllowMethods = "GET, POST, OPTIONS";
        http.Response.Headers.AccessControlAllowHeaders = "authorization, content-type, traceparent, tracestate";
        http.Response.Headers.AccessControlMaxAge = "86400";
      }
    }

    await next(context);
  }
}
