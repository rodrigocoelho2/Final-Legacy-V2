using System.Text;

namespace FinalLegacy.Library.Text;

public enum MarkupTokenKind
{
    Text,
    Color,
    Reset,
    Pause,
}

/// <summary>One piece of a text: plain text, a colour switch, a reset to the default colour or a dramatic pause.</summary>
public readonly record struct MarkupToken(MarkupTokenKind Kind, string Value = "");

/// <summary>
/// The tiny markup the texts are written in: <c>[gold]Final Legacy[/]</c>, <c>[pause]</c> for the original's slow
/// "..." and <c>[[</c> for a literal bracket. Unknown tags are left as they are.
/// </summary>
public static class Markup
{
    /// <summary>The colours a text can ask for, named after what the original game used them for.</summary>
    public static readonly IReadOnlySet<string> Colors = new HashSet<string>(StringComparer.Ordinal)
    {
        "gold",    // the title, damage you deal
        "amber",   // the hero's name, "you won"
        "red",     // enemies, damage you take
        "crimson", // the Warrior, HP
        "cyan",    // the Mage, weapons, magic
        "violet",  // the Assassin
        "green",   // the hero's HP bar
        "dim",     // hints
        "white",   // common items
        "blue",    // rare items
    };

    public static IEnumerable<MarkupToken> Parse(string text)
    {
        var buffer = new StringBuilder();
        var i = 0;
        while (i < text.Length)
        {
            var c = text[i];
            if (c == '[' && i + 1 < text.Length && text[i + 1] == '[')
            {
                buffer.Append('[');
                i += 2;
                continue;
            }

            if (c == '[')
            {
                var close = text.IndexOf(']', i + 1);
                if (close > i)
                {
                    var tag = text.Substring(i + 1, close - i - 1);
                    MarkupToken? token = tag switch
                    {
                        "/" => new MarkupToken(MarkupTokenKind.Reset),
                        "pause" => new MarkupToken(MarkupTokenKind.Pause),
                        _ when Colors.Contains(tag) => new MarkupToken(MarkupTokenKind.Color, tag),
                        _ => null,
                    };

                    if (token is { } known)
                    {
                        if (buffer.Length > 0)
                        {
                            yield return new MarkupToken(MarkupTokenKind.Text, buffer.ToString());
                            buffer.Clear();
                        }

                        yield return known;
                        i = close + 1;
                        continue;
                    }
                }
            }

            buffer.Append(c);
            i++;
        }

        if (buffer.Length > 0)
        {
            yield return new MarkupToken(MarkupTokenKind.Text, buffer.ToString());
        }
    }

    /// <summary>The text as it reads on screen, without tags.</summary>
    public static string Strip(string text) =>
        string.Concat(Parse(text).Where(t => t.Kind == MarkupTokenKind.Text).Select(t => t.Value));

    /// <summary>Makes text typed by the player (their name) safe to put inside markup.</summary>
    public static string Escape(string text) => text.Replace("[", "[[", StringComparison.Ordinal);

    /// <summary>Wraps text in a colour tag (or leaves it as is for an unknown colour).</summary>
    public static string Colorize(string text, string color) =>
        Colors.Contains(color) ? $"[{color}]{text}[/]" : text;
}
