using System.Net.Http.Json;

namespace BikeBuilder.Web.Services;

public class RatingsClient(HttpClient http)
{
  public async Task<List<RatingDto>> ListAsync(int bikeBuildId, CancellationToken ct = default) =>
      await http.GetFromJsonAsync<List<RatingDto>>($"/api/bikebuilds/{bikeBuildId}/ratings", ct) ?? [];

  public Task<HttpResponseMessage> CreateAsync(int bikeBuildId, CreateRatingRequest request, CancellationToken ct = default) =>
      http.PostAsJsonAsync($"/api/bikebuilds/{bikeBuildId}/ratings", request, ct);
}

public sealed record RatingDto(string Id, int Stars, string? Comment, string UserName, DateTimeOffset CreatedAt);

public sealed record CreateRatingRequest(int Stars, string? Comment, string BikeBuildName);
