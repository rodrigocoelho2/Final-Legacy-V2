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
    public static string Options(
        IReadOnlyList<string> labels, int selected, bool vertical, IReadOnlyList<string>? hints = null, string separator = "     ")
    {
        var parts = labels.Select((label, i) =>
        {
            var hint = hints is not null && i < hints.Count && !string.IsNullOrEmpty(hints[i]) ? $" [dim]{hints[i]}[/]" : "";
            return i == selected
                ? $"[gold]›[/] [gold][[{i + 1}] {label}[/]{hint}"
                : $"  [[{i + 1}] {label}{hint}";
        });
        var list = parts.ToList();
        var oneLine = " " + string.Join(separator, list);

        // A menu too wide for the window goes one option per line.
        return vertical || Markup.Strip(oneLine).Length > TextWidth ? " " + string.Join("\n ", list) : oneLine;
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

    /// <summary>Wraps a whole text (several lines) at <paramref name="width"/> visible characters.</summary>
    public static List<string> WrapText(string text, int width) =>
        text.Split('\n').SelectMany(line => Wrap(line, width)).ToList();

    /// <summary>A line of the left column and how wide it shows (sprite lines are 16, markup is measured).</summary>
    public readonly record struct Cell(string Text, int Width)
    {
        public static Cell Of(string markup) => new(markup, Markup.Strip(markup).Length);
    }

    /// <summary>Sprite lines as cells, shifted right by <paramref name="offset"/> columns.</summary>
    public static IEnumerable<Cell> SpriteCells(IReadOnlyList<string> lines, int offset = 0) =>
        lines.Select(l => new Cell(new string(' ', offset) + l, offset + 16));

    /// <summary>
    /// Puts two columns side by side: <paramref name="left"/> padded to <paramref name="leftWidth"/>, then
    /// <paramref name="right"/>. With no left column (no pixel art) the right one stands alone.
    /// </summary>
    public static string Columns(IEnumerable<Cell> left, int leftWidth, IReadOnlyList<string> right, int indent = 2, int gap = 3)
    {
        var leftList = left.ToList();
        var pad = new string(' ', indent);
        if (leftList.Count == 0) return string.Join("\n", right.Select(r => pad + r));

        var rows = Math.Max(leftList.Count, right.Count);
        var lines = new List<string>(rows);
        for (var i = 0; i < rows; i++)
        {
            var cell = i < leftList.Count ? leftList[i] : new Cell("", 0);
            var line = pad + cell.Text + new string(' ', Math.Max(0, leftWidth - cell.Width));
            lines.Add(i < right.Count ? line + new string(' ', gap) + right[i] : line.TrimEnd());
        }

        return string.Join("\n", lines);
    }

    private static string Center(string text, int width, string color)
    {
        var left = (width - text.Length) / 2;
        var right = width - text.Length - left;
        return $"{new string(' ', left)}[{color}]{text}[/]{new string(' ', right)}";
    }
}
