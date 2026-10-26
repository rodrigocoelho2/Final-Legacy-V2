namespace FinalLegacy.Library.Models;

/// <summary>
/// The four stats of the original game: attack, defense, agility and intelligence.
/// Classes have base stats, weapons have modifiers (which can be negative) and the hero's stats are their sum.
/// </summary>
public readonly record struct Stats(int Attack, int Defense, int Agility, int Intelligence)
{
    public static Stats Zero => default;

    public static Stats operator +(Stats a, Stats b) => new(
        a.Attack + b.Attack,
        a.Defense + b.Defense,
        a.Agility + b.Agility,
        a.Intelligence + b.Intelligence);

    /// <summary>No stat goes below zero (a -1 modifier on a 0 stat stays 0).</summary>
    public Stats AtLeastZero() => new(
        Math.Max(0, Attack),
        Math.Max(0, Defense),
        Math.Max(0, Agility),
        Math.Max(0, Intelligence));

    /// <summary>The stats in display order with their text keys (stat.attack, stat.defense…).</summary>
    public IReadOnlyList<(string Key, int Value)> Entries() =>
    [
        ("stat.attack", Attack),
        ("stat.defense", Defense),
        ("stat.agility", Agility),
        ("stat.intelligence", Intelligence),
    ];
}
