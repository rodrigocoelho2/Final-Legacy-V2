using FinalLegacy.Game.Terminal;

namespace FinalLegacy.Game.Ui;

/// <summary>The words a menu needs around it, in the chosen language.</summary>
public sealed record MenuTexts(string KeysHint, string LineHint, string Invalid, string InvalidAgain);

/// <summary>
/// A screen with a menu at the bottom. With a keyboard: arrows (or WASD) move, Enter or Space confirm and a number key
/// picks straight away. With piped input: the number of the option, one per line, like the original's scanf("%d")
/// — but a letter or a wrong number gets a friendly message instead of an endless loop.
/// </summary>
public sealed class MenuPrompt(GameTerminal terminal, MenuTexts texts)
{
    private string? _lastHeader;

    /// <param name="header">The text above the menu; typed out the first time the screen is drawn.</param>
    /// <param name="body">Draws the options (and anything under them) for the highlighted index, or -1 for none.</param>
    /// <param name="count">How many options there are.</param>
    /// <param name="start">The option highlighted at first.</param>
    /// <param name="before">Drawn at once above the header (the title box, already seen on the last screen).</param>
    public int Run(string header, Func<int, string> body, int count, int start = 0, string before = "")
    {
        return terminal.InputIsKeys
            ? WithKeys(before, header, body, count, start)
            : WithLines(before, header, body, count);
    }

    private int WithKeys(string before, string header, Func<int, string> body, int count, int selected)
    {
        terminal.Clear();
        terminal.Write(before);
        TypeOnce(header);
        terminal.Write(Footer(body(selected), texts.KeysHint));

        while (true)
        {
            var key = terminal.ReadKey();
            var next = selected;
            switch (key.Key)
            {
                case ConsoleKey.UpArrow or ConsoleKey.LeftArrow or ConsoleKey.W or ConsoleKey.A:
                    next = (selected - 1 + count) % count;
                    break;
                case ConsoleKey.DownArrow or ConsoleKey.RightArrow or ConsoleKey.S or ConsoleKey.D or ConsoleKey.Tab:
                    next = (selected + 1) % count;
                    break;
                case ConsoleKey.Enter or ConsoleKey.Spacebar:
                    return selected;
                default:
                    if (Number(key.KeyChar.ToString(), count) is { } picked)
                    {
                        return picked;
                    }

                    continue;
            }

            if (next == selected) continue;
            selected = next;
            terminal.Clear();
            terminal.Write(before + header);
            terminal.Write(Footer(body(selected), texts.KeysHint));
        }
    }

    private int WithLines(string before, string header, Func<int, string> body, int count)
    {
        terminal.Clear();
        terminal.Write(before);
        TypeOnce(header);
        var invalidAnswers = 0;

        while (true)
        {
            terminal.Write(Footer(body(-1), texts.LineHint));
            terminal.Write("[gold]›[/] ");
            var answer = terminal.ReadLine();
            if (Number(answer, count) is { } picked)
            {
                return picked;
            }

            invalidAnswers++;
            terminal.WriteLine();
            terminal.WriteLine($"  [red]{(invalidAnswers > 1 ? texts.InvalidAgain : texts.Invalid)}[/]");
        }
    }

    /// <summary>Types a header out the first time; when the same screen comes back (after a purchase, after the
    /// inventory), it appears at once.</summary>
    private void TypeOnce(string header)
    {
        if (header == _lastHeader) terminal.Write(header);
        else terminal.Type(header);
        _lastHeader = header;
    }

    private static string Footer(string body, string hint) => $"{body}\n\n  [dim]{hint}[/]\n";

    /// <summary>"2" → index 1, when it's one of the options.</summary>
    public static int? Number(string answer, int count) =>
        int.TryParse(answer, out var n) && n >= 1 && n <= count ? n - 1 : null;
}
