using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace BikeBuilder.API.Ratings.Functions;

// Answers CORS preflight for every route; CorsMiddleware adds the actual headers.
public class CorsPreflightFunction
{
  [Function("CorsPreflight")]
  public IActionResult Run(
      [HttpTrigger(AuthorizationLevel.Anonymous, "options", Route = "{*route}")] HttpRequest req) =>
      new NoContentResult();
}
