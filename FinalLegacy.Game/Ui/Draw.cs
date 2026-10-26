using System.Text;
using FinalLegacy.Library.Models;
using FinalLegacy.Library.Text;

namespace FinalLegacy.Game.Ui;

/// <summary>The pieces the screens are made of, as markup.</summary>
public static class Draw
{
    public const int Width = 56;

    /// <summary>The highest a stat goes with any class and weapon, so every bar shares one scale.</summary>
    public const int StatScale = 8;

    public static string Rule => $"[dim]{new string('─', Width)}[/]";

    /// <summary>The title in a double-line box, like the original's "-----|Bem-Vindo ao Final Legacy!|-----".</summary>
    public static string TitleBox(string tagline)
    {
        const string title = "F I N A L   L E G A C Y";
        var inner = Width - 2;
        return
            $"[dim]╔{new string('═', inner)}╗[/]\n" +
            $"[dim]║[/]{Center(title, inner, "gold")}[dim]║[/]\n" +
            $"[dim]║[/]{Center(tagline, inner, "dim")}[dim]║[/]\n" +
            $"[dim]╚{new string('═', inner)}╝[/]\n";
    }

    /// <summary>"████░░░░" — a stat out of <see cref="StatScale"/>.</summary>
    public static string StatBar(int value) =>
        new string('█', Math.Clamp(value, 0, StatScale)) + new string('░', StatScale - Math.Clamp(value, 0, StatScale));

    /// <summary>"ATK ████░░░░  DEF █████░░░ …" with the labels in the chosen language.</summary>
    public static string StatLine(Stats stats, Func<string, string> label, bool numbers = false)
    {
        return string.Join("  ", stats.Entries().Select(e =>
            $"{label(e.Key)} {StatBar(e.Value)}{(numbers ? $" {e.Value}" : string.Empty)}"));
    }

    /// <summary>An HP bar that turns from its colour to amber and red as it empties.</summary>
    public static string HpBar(string label, int hp, int max, string color, int width = 24)
    {
        var filled = max == 0 ? 0 : (int)Math.Ceiling((double)hp * width / max);
        var ratio = max == 0 ? 0 : (double)hp / max;
        var barColor = color == "green" && ratio <= 0.25 ? "red" : color == "green" && ratio <= 0.5 ? "amber" : color;
        return $"[crimson]{label}[/] [{barColor}]{new string('█', filled)}[/][dim]{new string('░', width - filled)}[/]  {hp}/{max}";
    }

    /// <summary>"[1] Sim    [2] Não" with the highlighted one marked; on one line or one per line.</summary>
    public static string Options(IReadOnlyList<string> labels, int selected, bool vertical, IReadOnlyList<string>? hints = null)
    {
        var parts = labels.Select((label, i) =>
        {
            var hint = hints is not null && i < hints.Count && !string.IsNullOrEmpty(hints[i]) ? $" [dim]{hints[i]}[/]" : "";
            return i == selected
                ? $"[gold]›[/] [gold][[{i + 1}] {label}[/]{hint}"
                : $"  [[{i + 1}] {label}{hint}";
        });
        return vertical ? " " + string.Join("\n ", parts) : " " + string.Join("     ", parts);
    }

    /// <summary>Pads text that may hold markup to a visible width.</summary>
    public static string Pad(string markup, int width)
    {
        var visible = Markup.Strip(markup).Length;
        return visible >= width ? markup : markup + new string(' ', width - visible);
    }

    /// <summary>Wraps every line of a text at <see cref="TextWidth"/> visible characters and indents it.</summary>
    public static string Indent(string text, string by = "  ")
    {
        var builder = new StringBuilder();
        var lines = text.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            if (i > 0) builder.Append('\n');
            var wrapped = Wrap(lines[i], TextWidth);
            builder.Append(string.Join("\n", wrapped.Select(l => l.Length == 0 ? l : by + l)));
        }

        return builder.ToString();
    }

    /// <summary>The longest a line of story gets, so it reads well in an 80-column window.</summary>
    public const int TextWidth = 74;

    /// <summary>Splits one line of markup into lines of at most <paramref name="width"/> visible characters.</summary>
    public static IReadOnlyList<string> Wrap(string line, int width)
    {
        if (Markup.Strip(line).Length <= width) return [line];

        var lines = new List<string>();
        var current = new StringBuilder();
        var visible = 0;
        foreach (var word in line.Split(' '))
        {
            var length = Markup.Strip(word).Length;
            if (visible > 0 && visible + 1 + length > width)
            {
                lines.Add(current.ToString());
                current.Clear();
                visible = 0;
            }

            if (visible > 0 || current.Length > 0)
            {
                current.Append(' ');
                visible++;
            }

            current.Append(word);
            visible += length;
        }

        lines.Add(current.ToString());
        return lines;
    }

    private static string Center(string text, int width, string color)
    {
        var left = (width - text.Length) / 2;
        var right = width - text.Length - left;
        return $"{new string(' ', left)}[{color}]{text}[/]{new string(' ', right)}";
    }
}
