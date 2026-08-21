using System.Text.RegularExpressions;
using Bogus;
using MovieShowcase.Core;
using MovieShowcase.Localization;
using MovieShowcase.Models;

namespace MovieShowcase.Generation;

public sealed partial class MovieGenerator
{
    private static readonly Regex TokenRegex = PlaceholderRegex();
    private readonly LocaleProvider _locales;

    public MovieGenerator(LocaleProvider locales) => _locales = locales;

    public IReadOnlyList<Movie> GeneratePage(GenerationParams p)
    {
        var locale = _locales.Get(p.Locale);
        long start = (long)p.Page * p.PageSize;

        var result = new List<Movie>(p.PageSize);
        for (int i = 0; i < p.PageSize; i++)
            result.Add(GenerateOne(locale, p, start + i));

        return result;
    }

    private Movie GenerateOne(LocaleData loc, GenerationParams p, long index)
    {
        var coreRng = new Pcg32(SeedDerivation.RecordSeed(p.Seed, index, SeedField.Core));
        var likesRng = new Pcg32(SeedDerivation.RecordSeed(p.Seed, index, SeedField.Likes));
        var reviewsRng = new Pcg32(SeedDerivation.RecordSeed(p.Seed, index, SeedField.Reviews));

        // Bogus needs its own locale code ("en_US"/"de"), not the culture tag ("en-US").
        var faker = new Faker(loc.BogusLocale)
        {
            Random = new Randomizer(
                (int)(SeedDerivation.RecordSeed(p.Seed, index, SeedField.Core) & 0x7FFFFFFF))
        };

        // Reviewer identities come from their own Faker so that changing the
        // review count cannot disturb cast/director names.
        var reviewFaker = new Faker(loc.BogusLocale)
        {
            Random = new Randomizer(
                (int)(SeedDerivation.RecordSeed(p.Seed, index, SeedField.Reviews) & 0x7FFFFFFF))
        };

        // ---------- core stream ----------
        var genre = loc.Genres[coreRng.NextInt(0, loc.Genres.Count)];
        var title = BuildTitle(loc, genre, coreRng);
        int year = coreRng.NextInt(1975, 2027);
        int runtime = coreRng.NextInt(82, 165);
        var rating = loc.AgeRatings[coreRng.NextInt(0, loc.AgeRatings.Count)];
        bool top10 = coreRng.NextDouble() < 0.08;

        int castSize = coreRng.NextInt(1, 5);
        var cast = new List<string>(castSize);
        for (int i = 0; i < castSize; i++) cast.Add(faker.Name.FullName());

        var director = faker.Name.FullName();
        var synopsis = BuildSynopsis(loc, coreRng);

        // ---------- likes stream ----------
        int likes = Probabilistic(p.AvgLikes, likesRng);

        // ---------- reviews stream ----------
        int reviewCount = Probabilistic(p.AvgReviews, reviewsRng);
        var reviews = new List<Review>(reviewCount);
        for (int i = 0; i < reviewCount; i++)
        {
            var template = loc.ReviewTemplates[reviewsRng.NextInt(0, loc.ReviewTemplates.Count)];
            var text = Fill(template, loc, genre, reviewsRng, avoidRepeats: false);
            reviews.Add(new Review(text, reviewFaker.Name.FullName(), reviewFaker.Company.CompanyName()));
        }

        return new Movie
        {
            Index = index + 1,
            Title = title,
            Genre = genre,
            Year = year,
            Cast = cast,
            Director = director,
            RuntimeMinutes = runtime,
            AgeRating = rating,
            IsTop10 = top10,
            Synopsis = synopsis,
            Likes = likes,
            Reviews = reviews
        };
    }

    // ---------- text building ----------

    private static string BuildTitle(LocaleData loc, string genre, Pcg32 rng)
    {
        int roll = rng.NextInt(0, loc.TotalWeight(genre));
        var (pattern, _) = loc.PickPattern(genre, roll);
        return Fill(pattern.Template, loc, genre, rng, avoidRepeats: true);
    }

    private static string BuildSynopsis(LocaleData loc, Pcg32 rng)
    {
        if (loc.SynopsisTemplates.Count == 0) return string.Empty;

        var template = loc.SynopsisTemplates[rng.NextInt(0, loc.SynopsisTemplates.Count)];
        return Fill(template, loc, genre: null, rng, avoidRepeats: true);
    }

    /// <summary>
    /// Replaces every {Slot} token. Each occurrence is resolved independently, so
    /// "{Noun} of {Noun}" yields two different nouns.
    /// </summary>
    private static string Fill(string template, LocaleData loc, string? genre, Pcg32 rng, bool avoidRepeats)
    {
        var used = avoidRepeats ? new HashSet<string>(StringComparer.OrdinalIgnoreCase) : null;

        return TokenRegex.Replace(template, match =>
        {
            var slot = match.Groups[1].Value;
            var pool = GetWordPool(loc, genre, slot);
            if (pool.Count == 0) return match.Value;

            var word = pool[rng.NextInt(0, pool.Count)];

            if (used is not null && pool.Count > 1)
            {
                for (int attempt = 0; attempt < 4 && used.Contains(word); attempt++)
                    word = pool[rng.NextInt(0, pool.Count)];
                used.Add(word);
            }

            return word;
        });
    }

    /// <summary>Genre lexicon first, then the shared lexicon.</summary>
    private static IReadOnlyList<string> GetWordPool(LocaleData loc, string? genre, string slot)
    {
        if (genre is not null && loc.Slot(genre, slot) is { Count: > 0 } genreWords)
            return genreWords;

        return loc.SharedLexicon.TryGetValue(slot, out var shared) ? shared : [];
    }

    private static int Probabilistic(double avg, Pcg32 rng)
    {
        int whole = (int)Math.Floor(avg);
        double frac = avg - whole;
        return whole + (rng.NextDouble() < frac ? 1 : 0);
    }

    [GeneratedRegex(@"\{(\w+)\}")]
    private static partial Regex PlaceholderRegex();
}