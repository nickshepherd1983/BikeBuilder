namespace BikeBuilder.API.Ratings.Models;

// Properties are nullable so validation can distinguish missing values and return 400.
public sealed record CreateRatingRequest
{
  public int? Stars { get; init; }
  public string? Comment { get; init; }
  public string? BikeBuildName { get; init; }
}
