using System.Net.Http.Json;

namespace BikeBuilder.Web.Services;

public class RatingsClient(HttpClient http)
{
  public async Task<List<RatingDto>> ListAsync(int bikeBuildId, CancellationToken ct = default) =>
      await http.GetFromJsonAsync<List<RatingDto>>($"/api/bikebuilds/{bikeBuildId}/ratings", ct) ?? [];

  public Task<HttpResponseMessage> CreateAsync(int bikeBuildId, CreateRatingRequest request, CancellationToken ct = default) =>
      http.PostAsJsonAsync($"/api/bikebuilds/{bikeBuildId}/ratings", request, ct);

  public async Task<Dictionary<int, int>> GetCountsAsync(IEnumerable<int> bikeBuildIds, CancellationToken ct = default)
  {
    var ids = string.Join(',', bikeBuildIds);
    if (ids.Length == 0)
      return [];

    var counts = await http.GetFromJsonAsync<List<RatingCountDto>>($"/api/bikebuilds/ratings/counts?ids={ids}", ct) ?? [];
    return counts.ToDictionary(count => int.Parse(count.BikeBuildId), count => count.Count);
  }
}

public sealed record RatingDto(string Id, int Stars, string? Comment, string UserName, DateTimeOffset CreatedAt);

public sealed record RatingCountDto(string BikeBuildId, int Count);

public sealed record CreateRatingRequest(int Stars, string? Comment, string BikeBuildName);
