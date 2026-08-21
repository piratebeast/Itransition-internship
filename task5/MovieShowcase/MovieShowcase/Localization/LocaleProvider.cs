using System.Collections.Concurrent;
using System.Text.Json;

namespace MovieShowcase.Localization;

public sealed class LocaleProvider
{
    public const string DefaultCulture = "en-US";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private readonly ConcurrentDictionary<string, LocaleData> _cache =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly string _root;

    public LocaleProvider(IWebHostEnvironment env)
        => _root = Path.Combine(env.WebRootPath, "locales");

    // ---------- public API ----------

    public LocaleData Get(string? culture)
    {
        if (string.IsNullOrWhiteSpace(culture)) culture = DefaultCulture;
        return _cache.GetOrAdd(culture, Load);
    }

    public IReadOnlyList<LocaleInfo> Available()
    {
        if (!Directory.Exists(_root)) return [];

        var list = new List<LocaleInfo>();
        foreach (var file in Directory.EnumerateFiles(_root, "*.json"))
        {
            var culture = Path.GetFileNameWithoutExtension(file);
            if (string.IsNullOrEmpty(culture)) continue;

            try
            {
                var d = Get(culture);
                list.Add(new LocaleInfo(
                    culture,
                    string.IsNullOrWhiteSpace(d.DisplayName) ? culture : d.DisplayName));
            }
            catch (InvalidDataException)
            {
                // a malformed locale file shouldn't remove every language from the dropdown
            }
        }

        return list.OrderBy(l => l.DisplayName, StringComparer.OrdinalIgnoreCase).ToList();
    }

    // ---------- loading ----------

    private LocaleData Load(string culture)
    {
        var path = Path.Combine(_root, culture + ".json");

        if (!File.Exists(path))
        {
            if (culture.Equals(DefaultCulture, StringComparison.OrdinalIgnoreCase))
                throw new FileNotFoundException($"Default locale file missing: {path}");
            return Get(DefaultCulture);
        }

        var data = JsonSerializer.Deserialize<LocaleData>(File.ReadAllText(path), JsonOpts)
                   ?? throw new InvalidDataException($"Could not parse {path}");

        Validate(data, path);
        data.BuildIndex();
        return data;
    }

    // ---------- validation ----------

    private static void Validate(LocaleData d, string path)
    {
        if (d.Genres.Count == 0) throw new InvalidDataException($"{path}: no genres");
        if (d.TitlePatterns.Count == 0) throw new InvalidDataException($"{path}: no titlePatterns");
        if (d.AgeRatings.Count == 0) throw new InvalidDataException($"{path}: no ageRatings");

        foreach (var p in d.TitlePatterns)
        {
            if (p.Weight <= 0)
                throw new InvalidDataException($"{path}: pattern '{p.Template}' has weight <= 0");

            var slots = ParseSlots(p.Template);
            if (slots.Length == 0)
                throw new InvalidDataException($"{path}: pattern '{p.Template}' has no slots");

            foreach (var g in d.Genres)
            {
                if (!p.FitsGenre(g)) continue;
                foreach (var slot in slots)
                    if (d.Slot(g, slot) is null)
                        throw new InvalidDataException(
                            $"{path}: genre '{g}' pattern '{p.Template}' has no words for slot '{slot}'");
            }
        }

        // every genre must have at least one usable pattern
        foreach (var g in d.Genres)
            if (!d.TitlePatterns.Any(p => p.FitsGenre(g)))
                throw new InvalidDataException($"{path}: genre '{g}' has no matching title pattern");
    }

    internal static string[] ParseSlots(string template)
    {
        var result = new List<string>();
        int i = 0;
        while ((i = template.IndexOf('{', i)) >= 0)
        {
            int end = template.IndexOf('}', i);
            if (end < 0) break;
            result.Add(template[(i + 1)..end]);
            i = end + 1;
        }
        return result.ToArray();
    }
}

public record LocaleInfo(string Culture, string DisplayName);