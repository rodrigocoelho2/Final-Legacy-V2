namespace FinalLegacy.Library.Enums;

/// <summary>The languages the game is written in. Each one has a Content/&lt;code&gt;.json file.</summary>
public enum Language
{
    Portuguese,
    English,
}

public static class LanguageExtensions
{
    /// <summary>The short code used for the content file and the --lang option.</summary>
    public static string Code(this Language language) => language switch
    {
        Language.Portuguese => "pt",
        Language.English => "en",
        _ => throw new ArgumentOutOfRangeException(nameof(language), language, null),
    };

    public static bool TryParse(string? code, out Language language)
    {
        foreach (var candidate in Enum.GetValues<Language>())
        {
            if (string.Equals(candidate.Code(), code?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                language = candidate;
                return true;
            }
        }

        language = default;
        return false;
    }
}
