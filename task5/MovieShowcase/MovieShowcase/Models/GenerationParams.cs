namespace MovieShowcase.Models;

public record GenerationParams(
    ulong Seed,
    string Locale,
    double AvgLikes,
    double AvgReviews,
    int Page,
    int PageSize
);