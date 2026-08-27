namespace BikeBuilder.API.Ratings.Models;

public sealed record RatingSummaryDto(string BikeBuildId, int Count, double AverageStars);

// Shape of the per-rating projection the summaries query pulls back from Cosmos.
public sealed record RatingStarsItem(string BikeBuildId, int Stars);
