using System.Text.Json.Serialization;

namespace MovieShowcase.Localization;

public sealed class LocaleData
{
    [JsonPropertyName("culture")]
    public string Culture { get; init; } = "";

    [JsonPropertyName("displayName")]
    public string DisplayName { get; init; } = "";

    [JsonPropertyName("bogusLocale")]
    public string BogusLocale { get; init; } = "en";

    [JsonPropertyName("genres")]
    public IReadOnlyList<string> Genres { get; init; } = [];

    [JsonPropertyName("ageRatings")]
    public IReadOnlyList<string> AgeRatings { get; init; } = [];

    [JsonPropertyName("titlePatterns")]
    public IReadOnlyList<TitlePattern> TitlePatterns { get; init; } = [];

    // genre -> slot -> words     e.g. Lexicon["Action"]["Adj"] = ["Iron", "Silent"]
    [JsonPropertyName("lexicon")]
    public IReadOnlyDictionary<string, Dictionary<string, List<string>>> Lexicon { get; init; }
        = new Dictionary<string, Dictionary<string, List<string>>>();

    // slot -> words, used when a genre's lexicon lacks the slot
    [JsonPropertyName("sharedLexicon")]
    public IReadOnlyDictionary<string, List<string>> SharedLexicon { get; init; }
        = new Dictionary<string, List<string>>();


    [JsonPropertyName("synopsisTemplates")]
    public IReadOnlyList<string> SynopsisTemplates { get; init; } = [];

    [JsonPropertyName("reviewTemplates")]
    public IReadOnlyList<string> ReviewTemplates { get; init; } = [];

    [JsonPropertyName("trailerCards")]
    public IReadOnlyList<string> TrailerCards { get; init; } = [];

    // genre -> taglines
    [JsonPropertyName("taglines")]
    public IReadOnlyDictionary<string, List<string>> Taglines { get; init; }
        = new Dictionary<string, List<string>>();

    // ---------- precomputed index (built once at load) ----------

    private Dictionary<string, GenrePatterns> _byGenre = new();

    internal void BuildIndex()
    {
        var map = new Dictionary<string, GenrePatterns>(StringComparer.Ordinal);

        foreach (var g in Genres)
        {
            var pats = TitlePatterns.Where(p => p.FitsGenre(g)).ToArray();
            var cum = new int[pats.Length];
            int total = 0;
            for (int i = 0; i < pats.Length; i++) { total += pats[i].Weight; cum[i] = total; }

            map[g] = new GenrePatterns(
                pats,
                cum,
                total,
                pats.ToDictionary(p => p.Template, p => LocaleProvider.ParseSlots(p.Template)));
        }

        _byGenre = map;
    }

    /// <summary>Weighted pattern pick. Pass a value in [0, TotalWeight(genre)).</summary>
    public (TitlePattern Pattern, string[] Slots) PickPattern(string genre, int roll)
    {
        var gp = Index(genre);

        if (roll < 0 || roll >= gp.TotalWeight)
            throw new ArgumentOutOfRangeException(nameof(roll),
                $"roll must be in [0, {gp.TotalWeight}) for genre '{genre}'.");

        int lo = 0, hi = gp.Cumulative.Length - 1;
        while (lo < hi)
        {
            int mid = (lo + hi) / 2;
            if (roll < gp.Cumulative[mid]) hi = mid; else lo = mid + 1;
        }

        var pat = gp.Patterns[lo];
        return (pat, gp.Slots[pat.Template]);
    }

    public int TotalWeight(string genre) => Index(genre).TotalWeight;

    private GenrePatterns Index(string genre)
    {
        if (_byGenre.Count == 0)
            throw new InvalidOperationException(
                $"BuildIndex() was not called on LocaleData for '{Culture}'.");

        if (!_byGenre.TryGetValue(genre, out var gp))
            throw new ArgumentException(
                $"Unknown genre '{genre}' for culture '{Culture}'.", nameof(genre));

        return gp;
    }

    private sealed record GenrePatterns(
        TitlePattern[] Patterns,
        int[] Cumulative,
        int TotalWeight,
        Dictionary<string, string[]> Slots);

    /// <summary>Words for a slot, preferring the genre lexicon, falling back to shared.</summary>
    public IReadOnlyList<string>? Slot(string genre, string slot)
    {
        if (Lexicon.TryGetValue(genre, out var g) && g.TryGetValue(slot, out var w) && w.Count > 0)
            return w;
        if (SharedLexicon.TryGetValue(slot, out var s) && s.Count > 0)
            return s;
        return null;
    }
}

public sealed record TitlePattern
{
    [JsonPropertyName("weight")]
    public int Weight { get; init; } = 1;

    [JsonPropertyName("template")]
    public string Template { get; init; } = "";

    /// <summary>Genres this pattern suits. Empty means it fits any genre.</summary>
    [JsonPropertyName("genres")]
    public IReadOnlyList<string> Genres { get; init; } = [];

    public bool FitsGenre(string genre)
        => Genres.Count == 0 || Genres.Contains(genre);
}