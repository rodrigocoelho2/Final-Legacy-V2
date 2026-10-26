using System.Text;
using FinalLegacy.Game.Terminal;
using FinalLegacy.Library.Sprites;

namespace FinalLegacy.Game.Ui;

/// <summary>
/// Draws pixel art in the terminal with half blocks: every character is two pixels, the top one as the letter's
/// colour (▀) and the bottom one as its background, so a 16×24 sprite takes 16 columns and 12 rows and the pixels
/// come out square.
/// </summary>
public static class PixelArt
{
    private const string Esc = "\u001b[";

    /// <summary>The picture as console lines, each exactly <see cref="PixelGrid.Width"/> characters wide.</summary>
    public static IReadOnlyList<string> Lines(PixelGrid grid, ArtMode mode, int fromRow = 0, int toRow = -1)
    {
        if (mode == ArtMode.None) return [];
        if (toRow < 0) toRow = grid.Height;

        var lines = new List<string>();
        for (var y = fromRow; y < toRow; y += 2)
        {
            var line = new StringBuilder();
            for (var x = 0; x < grid.Width; x++)
            {
                var top = grid[x, y];
                var bottom = y + 1 < grid.Height ? grid[x, y + 1] : null;
                if (top is null && bottom is null)
                {
                    line.Append(' ');
                    continue;
                }

                if (top is { } t && bottom is { } b) line.Append(Fg(t, mode)).Append(Bg(b, mode)).Append('▀');
                else if (top is { } onlyTop) line.Append(Fg(onlyTop, mode)).Append('▀');
                else line.Append(Fg(bottom!.Value, mode)).Append('▄');
                line.Append(Esc).Append("0m");
            }

            lines.Add(line.ToString());
        }

        return lines;
    }

    /// <summary>Only the rows that have something in them (item icons sit in a mostly empty 16×24 canvas).</summary>
    public static IReadOnlyList<string> Cropped(PixelGrid grid, ArtMode mode)
    {
        var rows = Enumerable.Range(0, grid.Height).Where(y => Enumerable.Range(0, grid.Width).Any(x => grid[x, y] is not null)).ToList();
        if (rows.Count == 0) return [];
        var from = rows[0] - rows[0] % 2;
        var to = rows[^1] + 1;
        return Lines(grid, mode, from, Math.Min(grid.Height, to + to % 2));
    }

    private static string Fg(Rgb c, ArtMode mode) =>
        mode == ArtMode.TrueColor ? $"{Esc}38;2;{c.R};{c.G};{c.B}m" : $"{Esc}38;5;{To256(c)}m";

    private static string Bg(Rgb c, ArtMode mode) =>
        mode == ArtMode.TrueColor ? $"{Esc}48;2;{c.R};{c.G};{c.B}m" : $"{Esc}48;5;{To256(c)}m";

    /// <summary>The nearest colour of the 256-colour palette (its 6×6×6 cube or its greys).</summary>
    public static int To256(Rgb c)
    {
        static int Level(byte v) => (int)Math.Round(v / 255.0 * 5);
        int[] steps = [0, 95, 135, 175, 215, 255];
        var (r, g, b) = (Level(c.R), Level(c.G), Level(c.B));
        var cube = 16 + 36 * r + 6 * g + b;
        var cubeError = Sq(steps[r] - c.R) + Sq(steps[g] - c.G) + Sq(steps[b] - c.B);

        var grey = (c.R + c.G + c.B) / 3;
        var greyIndex = Math.Clamp((int)Math.Round((grey - 8) / 10.0), 0, 23);
        var greyValue = 8 + greyIndex * 10;
        var greyError = Sq(greyValue - c.R) + Sq(greyValue - c.G) + Sq(greyValue - c.B);
        return greyError < cubeError ? 232 + greyIndex : cube;

        static int Sq(int v) => v * v;
    }
}
