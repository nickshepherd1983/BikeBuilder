using BikeBuilder.API.Ratings.Models;

namespace BikeBuilder.API.Ratings.Functions;

public class RatingsFunctions(Container container, IEventPublisher eventPublisher)
{
  const int MaxCommentLength = 1000;

  static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

  [Function("CreateRating")]
  public async Task<IActionResult> CreateRatingAsync(
      [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "bikebuilds/{bikeBuildId}/ratings")] HttpRequest req,
      string bikeBuildId, FunctionContext context)
  {
    CreateRatingRequest? request;
    try
    {
      request = await JsonSerializer.DeserializeAsync<CreateRatingRequest>(
          req.Body, WebJson, context.CancellationToken);
    }
    catch (JsonException)
    {
      return new BadRequestObjectResult("Request body is not valid JSON.");
    }

    if (request?.Stars is not (>= 1 and <= 5))
      return new BadRequestObjectResult("stars is required and must be between 1 and 5.");

    if (request.Comment?.Length > MaxCommentLength)
    {
      return new BadRequestObjectResult($"comment must be at most {MaxCommentLength} characters.");
    }

    if (string.IsNullOrWhiteSpace(request.BikeBuildName))
      return new BadRequestObjectResult("bikeBuildName is required.");

    var user = (ClaimsPrincipal)context.Items[JwtAuthenticationMiddleware.UserContextKey];
    var userId = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)!.Value;
    // Real Auth0 access tokens usually carry only sub; an Auth0 Action adding a name claim
    // upgrades the display name without any change here.
    var userName = user.FindFirst("name")?.Value ?? user.FindFirst("nickname")?.Value ?? userId;

    var document = new RatingDocument
    {
      Id = Guid.NewGuid().ToString(),
      BikeBuildId = bikeBuildId,
      Stars = request.Stars.Value,
      Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment,
      UserId = userId,
      UserName = userName,
      CreatedAt = DateTimeOffset.UtcNow
    };

    await container.CreateItemAsync(document, new PartitionKey(bikeBuildId), cancellationToken: context.CancellationToken);

    await eventPublisher.PublishAsync(ServiceBusMessageTypes.RatingCreated, new RatingCreatedEvent
    {
      RatingId = document.Id,
      BikeBuildId = document.BikeBuildId,
      BikeBuildName = request.BikeBuildName,
      Stars = document.Stars,
      UserName = document.UserName,
      CreatedAt = document.CreatedAt
    }, context.CancellationToken);

    return new CreatedResult($"/api/bikebuilds/{bikeBuildId}/ratings/{document.Id}", document);
  }

  [Function("ListRatings")]
  public async Task<IActionResult> ListRatingsAsync(
      [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "bikebuilds/{bikeBuildId}/ratings")] HttpRequest req,
      string bikeBuildId, FunctionContext context)
  {
    var query = new QueryDefinition(
            "SELECT * FROM c WHERE c.bikeBuildId = @bikeBuildId ORDER BY c.createdAt DESC")
        .WithParameter("@bikeBuildId", bikeBuildId);

    var ratings = new List<RatingDocument>();
    using var iterator = container.GetItemQueryIterator<RatingDocument>(query,
        requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(bikeBuildId) });
    while (iterator.HasMoreResults)
    {
      ratings.AddRange(await iterator.ReadNextAsync(context.CancellationToken));
    }

    return new OkObjectResult(ratings);
  }

  [Function("GetRatingSummaries")]
  public async Task<IActionResult> GetRatingSummariesAsync(
      [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "bikebuilds/ratings/summaries")] HttpRequest req,
      FunctionContext context)
  {
    var ids = req.Query["ids"].ToString()
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    if (ids.Length == 0)
      return new OkObjectResult(Array.Empty<RatingSummaryDto>());

    // Cross-partition on purpose: summaries span many bikeBuildId partitions. Aggregating in
    // memory instead of GROUP BY keeps the query within what the Cosmos emulator supports.
    var query = new QueryDefinition(
            "SELECT c.bikeBuildId, c.stars FROM c WHERE ARRAY_CONTAINS(@ids, c.bikeBuildId)")
        .WithParameter("@ids", ids);

    var ratings = new List<RatingStarsItem>();
    using var iterator = container.GetItemQueryIterator<RatingStarsItem>(query);
    while (iterator.HasMoreResults)
    {
      ratings.AddRange(await iterator.ReadNextAsync(context.CancellationToken));
    }

    var summaries = ratings
        .GroupBy(rating => rating.BikeBuildId)
        .Select(group => new RatingSummaryDto(group.Key, group.Count(), group.Average(rating => (double)rating.Stars)))
        .ToList();

    return new OkObjectResult(summaries);
  }
}
