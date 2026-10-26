using System.Text.RegularExpressions;
using FinalLegacy.Library.Enums;

namespace FinalLegacy.Library.Combat;

/// <summary>Dice in D&amp;D notation: "1d8", "2d6+2", "2d4+2".</summary>
public readonly partial record struct Dice(int Count, int Sides, int Bonus = 0)
{
    public static Dice Parse(string text)
    {
        var match = Notation().Match(text.Replace(" ", ""));
        if (!match.Success) throw new FormatException($"'{text}' is not dice notation (like 1d8 or 2d6+2).");
        var bonus = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : 0;
        return new Dice(int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value), bonus);
    }

    /// <summary>The same dice with a different flat bonus.</summary>
    public Dice WithBonus(int bonus) => this with { Bonus = bonus };

    /// <summary>Rolls every die and adds the bonus. <paramref name="critical"/> doubles the dice, as in D&amp;D.</summary>
    public DamageRoll Roll(IRandomSource random, bool critical = false)
    {
        var count = critical ? Count * 2 : Count;
        var rolls = new int[count];
        for (var i = 0; i < count; i++) rolls[i] = Die(random, Sides);
        return new DamageRoll(this with { Count = count }, rolls);
    }

    public double Average => Count * (Sides + 1) / 2.0 + Bonus;

    public override string ToString() =>
        $"{Count}d{Sides}{(Bonus > 0 ? "+" + Bonus : Bonus < 0 ? Bonus.ToString() : "")}";

    /// <summary>One die from 1 to <paramref name="sides"/>.</summary>
    public static int Die(IRandomSource random, int sides) => 1 + (int)(random.NextDouble() * sides);

    [GeneratedRegex(@"^(\d+)d(\d+)([+-]\d+)?$")]
    private static partial Regex Notation();
}

/// <summary>A damage or healing roll: the dice as rolled and the total (never below 1).</summary>
public sealed record DamageRoll(Dice Dice, IReadOnlyList<int> Rolls)
{
    public int Total => Math.Max(1, Rolls.Sum() + Dice.Bonus);

    /// <summary>"1d10+3 (7+3)".</summary>
    public string Detail => $"{Dice} ({string.Join("+", Rolls)}{(Dice.Bonus > 0 ? "+" + Dice.Bonus : Dice.Bonus < 0 ? Dice.Bonus.ToString() : "")})";
}

/// <summary>
/// A d20 roll against a target: one die, or two with advantage or disadvantage. A natural 20 always hits and is
/// critical (or 19–20 with a keen weapon); a natural 1 always misses.
/// </summary>
public sealed record D20Roll(IReadOnlyList<int> Dice, RollMode Mode, int Bonus, int Target, int CritFrom = 20)
{
    public int Natural => Mode switch
    {
        RollMode.Advantage => Dice.Max(),
        RollMode.Disadvantage => Dice.Min(),
        _ => Dice[0],
    };

    public int Total => Natural + Bonus;
    public bool Critical => Natural >= CritFrom;
    public bool Fumble => Natural == 1;
    public bool Success => !Fumble && (Critical || Total >= Target);

    public static D20Roll Roll(IRandomSource random, RollMode mode, int bonus, int target, int critFrom = 20)
    {
        var dice = mode == RollMode.Normal
            ? new[] { Combat.Dice.Die(random, 20) }
            : new[] { Combat.Dice.Die(random, 20), Combat.Dice.Die(random, 20) };
        return new D20Roll(dice, mode, bonus, target, critFrom);
    }

    /// <summary>Advantage and disadvantage cancel out, as in D&amp;D.</summary>
    public static RollMode ModeOf(bool advantage, bool disadvantage) =>
        advantage == disadvantage ? RollMode.Normal : advantage ? RollMode.Advantage : RollMode.Disadvantage;
}
