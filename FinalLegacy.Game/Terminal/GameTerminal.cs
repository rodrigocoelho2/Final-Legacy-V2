using System.Runtime.InteropServices;
using System.Text;
using FinalLegacy.Library.Text;

namespace FinalLegacy.Game.Terminal;

/// <summary>The player closed the input (end of a piped script, Ctrl+D/Ctrl+Z): time to leave quietly.</summary>
public sealed class EndOfInputException : Exception;

/// <summary>
/// The console, without the original's Windows-only calls (windows.h, conio.h, system("cls")): ANSI colours, the
/// typewriter text (any key shows the rest at once) and keys or typed lines for input. It works on Windows, Linux and
/// macOS, and still runs with piped input and output (no colours, no animation, numbers typed one per line), which is
/// how the CI plays the game.
/// </summary>
public sealed class GameTerminal
{
    private const string Esc = "\u001b[";

    private static readonly Dictionary<string, string> AnsiColors = new()
    {
        ["gold"] = "1;33",
        ["amber"] = "0;33",
        ["red"] = "0;31",
        ["crimson"] = "1;31",
        ["cyan"] = "1;36",
        ["violet"] = "1;35",
        ["green"] = "1;32",
        ["dim"] = "2",
    };

    public GameTerminal(bool color, bool animate, int charDelayMs)
    {
        InputIsKeys = !Console.IsInputRedirected;
        OutputIsScreen = !Console.IsOutputRedirected;
        Color = color && OutputIsScreen;
        Animate = animate && OutputIsScreen && InputIsKeys;
        CharDelayMs = Math.Max(0, charDelayMs);
    }

    /// <summary>True when the player presses keys (arrows, Enter); false when lines are piped in.</summary>
    public bool InputIsKeys { get; }

    public bool OutputIsScreen { get; }
    public bool Color { get; private set; }
    public bool Animate { get; }
    public int CharDelayMs { get; }

    /// <summary>The original's [pause]: the slow "..." after the hero's name.</summary>
    public int PauseMs { get; init; } = 500;

    public void Start()
    {
        TrySet(() => Console.OutputEncoding = Encoding.UTF8);
        if (InputIsKeys) TrySet(() => Console.InputEncoding = Encoding.UTF8);

        if (OutputIsScreen)
        {
            TrySet(() => Console.Title = "Final Legacy");
            if (OperatingSystem.IsWindows() && !WindowsConsole.EnableAnsi())
            {
                Color = false;
            }
        }

        HideCursor();
        Console.CancelKeyPress += (_, _) => Restore();
    }

    /// <summary>Puts the console back as it was: default colour and a visible cursor.</summary>
    public void Restore()
    {
        if (!OutputIsScreen) return;
        Console.Write($"{Esc}0m{Esc}?25h");
        Console.Out.Flush();
    }

    public void HideCursor()
    {
        if (OutputIsScreen && InputIsKeys) Console.Write($"{Esc}?25l");
    }

    public void ShowCursor()
    {
        if (OutputIsScreen) Console.Write($"{Esc}?25h");
    }

    public void Clear()
    {
        if (OutputIsScreen)
        {
            Console.Write($"{Esc}2J{Esc}3J{Esc}H");
        }
        else
        {
            Console.WriteLine();
            Console.WriteLine(new string('─', 60));
        }
    }

    /// <summary>Writes markup at once.</summary>
    public void Write(string markup) => Console.Write(Render(markup));

    public void WriteLine(string markup = "") => Console.WriteLine(Render(markup));

    /// <summary>
    /// Writes markup letter by letter, like the original's prints(). A key press shows the rest at once (the key is
    /// swallowed so it doesn't also answer the next question).
    /// </summary>
    public void Type(string markup)
    {
        if (!Animate || CharDelayMs == 0)
        {
            Console.Write(Render(markup));
            return;
        }

        var skip = false;
        foreach (var token in Markup.Parse(markup))
        {
            switch (token.Kind)
            {
                case MarkupTokenKind.Text:
                    foreach (var c in token.Value)
                    {
                        Console.Write(c);
                        if (skip) continue;
                        Console.Out.Flush();
                        skip = Wait(c is '\n' or ' ' ? CharDelayMs / 2 : CharDelayMs);
                    }

                    break;
                case MarkupTokenKind.Pause:
                    if (!skip)
                    {
                        Console.Out.Flush();
                        skip = Wait(PauseMs);
                    }

                    break;
                default:
                    Console.Write(Code(token));
                    break;
            }
        }

        Console.Out.Flush();
    }

    /// <summary>The next key pressed (only when <see cref="InputIsKeys"/>).</summary>
    public ConsoleKeyInfo ReadKey() => Console.ReadKey(intercept: true);

    /// <summary>The next typed line (trimmed). Throws <see cref="EndOfInputException"/> when the input is closed.</summary>
    public string ReadLine()
    {
        ShowCursor();
        try
        {
            var line = Console.ReadLine() ?? throw new EndOfInputException();
            if (!InputIsKeys)
            {
                Console.WriteLine(line); // Echo piped answers so a recorded game reads like a real one.
            }

            return line.Trim();
        }
        finally
        {
            HideCursor();
        }
    }

    /// <summary>Waits for Enter (piped) or any key.</summary>
    public void WaitForContinue()
    {
        if (InputIsKeys)
        {
            ReadKey();
        }
        else
        {
            ReadLine();
        }
    }

    /// <summary>Markup to console text: ANSI colours when allowed, nothing otherwise.</summary>
    public string Render(string markup)
    {
        var builder = new StringBuilder();
        foreach (var token in Markup.Parse(markup))
        {
            builder.Append(token.Kind == MarkupTokenKind.Text ? token.Value : Code(token));
        }

        return builder.ToString();
    }

    private string Code(MarkupToken token)
    {
        if (!Color) return string.Empty;
        return token.Kind switch
        {
            MarkupTokenKind.Color when AnsiColors.TryGetValue(token.Value, out var code) => $"{Esc}0m{Esc}{code}m",
            MarkupTokenKind.Reset => $"{Esc}0m",
            _ => string.Empty,
        };
    }

    /// <summary>Sleeps, but returns true early if a key was pressed (and swallows it).</summary>
    private bool Wait(int milliseconds)
    {
        var until = Environment.TickCount64 + milliseconds;
        do
        {
            if (KeyWaiting())
            {
                while (KeyWaiting()) Console.ReadKey(intercept: true);
                return true;
            }

            Thread.Sleep(Math.Min(10, milliseconds));
        }
        while (Environment.TickCount64 < until);

        return false;
    }

    private bool KeyWaiting()
    {
        try
        {
            return InputIsKeys && Console.KeyAvailable;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static void TrySet(Action set)
    {
        try
        {
            set();
        }
        catch (Exception e) when (e is IOException or PlatformNotSupportedException or InvalidOperationException)
        {
            // Some hosts don't let the title or encodings change; the game works without it.
        }
    }

    /// <summary>Turns on ANSI escape codes in the classic Windows console (Windows Terminal already has them).</summary>
    private static class WindowsConsole
    {
        private const int StdOutputHandle = -11;
        private const uint EnableVirtualTerminalProcessing = 0x0004;

        public static bool EnableAnsi()
        {
            var handle = GetStdHandle(StdOutputHandle);
            return GetConsoleMode(handle, out var mode)
                && ((mode & EnableVirtualTerminalProcessing) != 0
                    || SetConsoleMode(handle, mode | EnableVirtualTerminalProcessing));
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int handle);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetConsoleMode(IntPtr handle, out uint mode);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleMode(IntPtr handle, uint mode);
    }
}
