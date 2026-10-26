using FinalLegacy.Library.Enums;

namespace FinalLegacy.Game;

/// <summary>What can be set when starting the game (final-legacy --help).</summary>
public sealed record GameOptions(Language? Language, bool Fast, bool Color, int? Seed, bool Help, string? Error)
{
    public const string Usage = """
        Final Legacy — a text-based RPG

        Usage: final-legacy [options]

          --lang pt|en   Skip the language menu
          --fast         Show text at once instead of letter by letter
          --no-color     No colours (also when NO_COLOR is set)
          --seed <n>     Same dice every time, to replay a game
          -h, --help     This help

        Keys: arrows (or WASD) to choose, Enter to confirm, a number to pick at once.
        Any key while text is being written shows the rest at once.
        """;

    public static GameOptions Parse(IReadOnlyList<string> args)
    {
        Language? language = null;
        var fast = false;
        var color = string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NO_COLOR"));
        int? seed = null;

        for (var i = 0; i < args.Count; i++)
        {
            switch (args[i])
            {
                case "--lang" or "-l" when i + 1 < args.Count:
                    if (!LanguageExtensions.TryParse(args[++i], out var parsed))
                        return Failed($"Unknown language '{args[i]}' (use pt or en).");
                    language = parsed;
                    break;
                case "--fast":
                    fast = true;
                    break;
                case "--no-color":
                    color = false;
                    break;
                case "--seed" when i + 1 < args.Count:
                    if (!int.TryParse(args[++i], out var s))
                        return Failed($"The seed must be a whole number, not '{args[i]}'.");
                    seed = s;
                    break;
                case "-h" or "--help":
                    return new GameOptions(null, false, color, null, Help: true, Error: null);
                default:
                    return Failed($"Unknown option '{args[i]}'.");
            }
        }

        return new GameOptions(language, fast, color, seed, Help: false, Error: null);
    }

    private static GameOptions Failed(string error) => new(null, false, false, null, Help: false, error);
}
