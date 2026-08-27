using System.Net.Http.Json;

namespace BikeBuilder.Web.Services;

public class RatingsClient(HttpClient http)
{
  public async Task<List<RatingDto>> ListAsync(int bikeBuildId, CancellationToken ct = default) =>
      await http.GetFromJsonAsync<List<RatingDto>>($"/api/bikebuilds/{bikeBuildId}/ratings", ct) ?? [];

  public Task<HttpResponseMessage> CreateAsync(int bikeBuildId, CreateRatingRequest request, CancellationToken ct = default) =>
      http.PostAsJsonAsync($"/api/bikebuilds/{bikeBuildId}/ratings", request, ct);

  public async Task<Dictionary<int, RatingSummaryDto>> GetSummariesAsync(IEnumerable<int> bikeBuildIds, CancellationToken ct = default)
  {
    var ids = string.Join(',', bikeBuildIds);
    if (ids.Length == 0)
      return [];

    var summaries = await http.GetFromJsonAsync<List<RatingSummaryDto>>($"/api/bikebuilds/ratings/summaries?ids={ids}", ct) ?? [];
    return summaries.ToDictionary(summary => int.Parse(summary.BikeBuildId));
  }
}

public sealed record RatingDto(string Id, int Stars, string? Comment, string UserName, DateTimeOffset CreatedAt);

public sealed record RatingSummaryDto(string BikeBuildId, int Count, double AverageStars);

public sealed record CreateRatingRequest(int Stars, string? Comment, string BikeBuildName);
