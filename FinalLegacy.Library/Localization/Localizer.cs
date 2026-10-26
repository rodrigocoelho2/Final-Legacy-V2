using System.Text;
using FinalLegacy.Library.Enums;

namespace FinalLegacy.Library.Localization;

/// <summary>
/// The texts of one language (Content/pt.json or Content/en.json): a flat map of keys to markup strings with
/// <c>{placeholders}</c>.
/// </summary>
public sealed class Localizer
{
    private readonly IReadOnlyDictionary<string, string> _texts;

    public Localizer(Language language, IReadOnlyDictionary<string, string> texts)
    {
        Language = language;
        _texts = texts;
    }

    public Language Language { get; }

    public IEnumerable<string> Keys => _texts.Keys;

    public bool Has(string key) => _texts.ContainsKey(key);

    /// <summary>The text for a key. A missing key shows up as «key» instead of crashing the game.</summary>
    public string this[string key] => _texts.TryGetValue(key, out var text) ? text : $"«{key}»";

    /// <summary>The text for a key with its placeholders filled in, e.g. ("damage", 6) for {damage}.</summary>
    public string Format(string key, params (string Name, object? Value)[] values) => Fill(this[key], values);

    /// <summary>Fills <c>{name}</c> placeholders in any text; unknown placeholders are left as they are.</summary>
    public static string Fill(string text, params (string Name, object? Value)[] values)
    {
        if (values.Length == 0 || !text.Contains('{'))
        {
            return text;
        }

        var result = new StringBuilder(text);
        foreach (var (name, value) in values)
        {
            result.Replace("{" + name + "}", value?.ToString() ?? string.Empty);
        }

        return result.ToString();
    }

    /// <summary>The text with its first letter in upper case ("o Esqueleto" → "O Esqueleto").</summary>
    public static string Capitalize(string text) =>
        string.IsNullOrEmpty(text) ? text : char.ToUpperInvariant(text[0]) + text[1..];
}
