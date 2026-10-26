using System.Globalization;

namespace FinalLegacy.Library.Sprites;

/// <summary>A colour, from "#rrggbb".</summary>
public readonly record struct Rgb(byte R, byte G, byte B)
{
    public static Rgb Parse(string hex)
    {
        var h = hex.TrimStart('#');
        if (h.Length != 6) throw new FormatException($"'{hex}' is not a #rrggbb colour.");
        return new Rgb(
            byte.Parse(h[..2], NumberStyles.HexNumber),
            byte.Parse(h[2..4], NumberStyles.HexNumber),
            byte.Parse(h[4..], NumberStyles.HexNumber));
    }

    public override string ToString() => $"#{R:x2}{G:x2}{B:x2}";
}

/// <summary>A finished picture: a grid of colours, null where it is see-through.</summary>
public sealed class PixelGrid
{
    private readonly Rgb?[] _pixels;

    public PixelGrid(int width, int height)
    {
        Width = width;
        Height = height;
        _pixels = new Rgb?[width * height];
    }

    public int Width { get; }
    public int Height { get; }

    public Rgb? this[int x, int y]
    {
        get => _pixels[y * Width + x];
        set => _pixels[y * Width + x] = value;
    }

    public bool IsEmpty => _pixels.All(p => p is null);
}

/// <summary>
/// One layer of pixel art from Content/sprites.json: rows of palette letters, '.' for see-through. Rows that aren't
/// listed are empty, so a helmet only needs the top rows.
/// </summary>
public sealed class Sprite(IReadOnlyDictionary<int, string> rows)
{
    public IReadOnlyDictionary<int, string> Rows { get; } = rows;
}

/// <summary>
/// The palette and every sprite. Pictures are made by drawing layers on top of each other — the hero's body, then
/// what they wear — like a paper doll, so changing a piece of armour changes the look.
/// </summary>
public sealed class SpriteBook(IReadOnlyDictionary<char, Rgb> palette, IReadOnlyDictionary<string, Sprite> sprites)
{
    public const int Width = 16;
    public const int Height = 24;

    public IReadOnlyDictionary<char, Rgb> Palette { get; } = palette;
    public IReadOnlyDictionary<string, Sprite> Sprites { get; } = sprites;

    public bool Has(string id) => Sprites.ContainsKey(id);

    /// <summary>Draws the layers in order; <paramref name="recolor"/> swaps palette letters (the class's hair).</summary>
    public PixelGrid Compose(IEnumerable<string> layers, IReadOnlyDictionary<char, Rgb>? recolor = null)
    {
        var grid = new PixelGrid(Width, Height);
        foreach (var id in layers)
        {
            if (!Sprites.TryGetValue(id, out var sprite)) continue;
            foreach (var (y, row) in sprite.Rows)
            {
                if (y is < 0 or >= Height) continue;
                for (var x = 0; x < Math.Min(row.Length, Width); x++)
                {
                    var key = row[x];
                    if (key == '.') continue;
                    if (recolor is not null && recolor.TryGetValue(key, out var swapped)) grid[x, y] = swapped;
                    else if (Palette.TryGetValue(key, out var color)) grid[x, y] = color;
                }
            }
        }

        return grid;
    }

    public PixelGrid Draw(string id) => Compose([id]);

    /// <summary>Palette letters a sprite uses that the palette doesn't have (for the content tests).</summary>
    public IEnumerable<string> UnknownColors() =>
        Sprites.SelectMany(s => s.Value.Rows.Values.SelectMany(r => r)
            .Where(c => c != '.' && !Palette.ContainsKey(c))
            .Distinct()
            .Select(c => $"{s.Key}: '{c}'"));

    /// <summary>Rows that are too long or outside the 16×24 canvas.</summary>
    public IEnumerable<string> BadRows() =>
        Sprites.SelectMany(s => s.Value.Rows
            .Where(r => r.Key is < 0 or >= Height || r.Value.Length != Width)
            .Select(r => $"{s.Key}: row {r.Key} is {r.Value.Length} wide"));
}
