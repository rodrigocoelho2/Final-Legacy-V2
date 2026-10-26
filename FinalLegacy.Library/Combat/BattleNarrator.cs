using FinalLegacy.Library.Enums;
using FinalLegacy.Library.Localization;
using FinalLegacy.Library.Models;

namespace FinalLegacy.Library.Combat;

/// <summary>
/// Turns a round into the lines under the HP bars, dice included: "Atacas: d20 14+5 = 19 vs CA 13 — acertas!
/// 1d10+3 (7+3) → 10 de dano cortante."
/// </summary>
public sealed class BattleNarrator(Localizer texts)
{
    public IReadOnlyList<string> Describe(Battle battle, IEnumerable<RoundEvent> events) =>
        events.Select(e => Describe(battle.Enemy, battle.Hero, e)).ToList();

    public string Describe(EnemyType enemy, Hero hero, RoundEvent e)
    {
        var the = texts[$"enemy.{enemy.Id}.the"];
        var heroAttacks = e.Kind == RoundEventKind.HeroAttack;
        var type = heroAttacks ? hero.Weapon.DamageType : enemy.DamageType;
        (string, object?)[] values =
        [
            ("enemy", texts[$"enemy.{enemy.Id}"]),
            ("the", the),
            ("The", Localizer.Capitalize(the)),
            ("roll", e.Roll is { } r ? RollText(r) : ""),
            ("ac", e.Roll?.Target),
            ("dc", e.Roll?.Target),
            ("damage", e.Damage?.Total),
            ("damageRoll", e.Damage?.Detail),
            ("type", texts[$"damage.{type}"]),
            ("item", e.Item is { } item ? texts[$"item.{item.Id}"] : ""),
            ("heal", e.Damage?.Total),
            ("healRoll", e.Damage?.Detail),
        ];

        return texts.Format(Key(e), values);
    }

    /// <summary>"d20 14+5 = 19", or "d20 4|15 (vantagem)+5 = 20" with the kept die highlighted.</summary>
    public string RollText(D20Roll roll)
    {
        string Die(int value, bool kept)
        {
            var color = !kept ? "dim" : value == 20 ? "gold" : value == 1 ? "red" : null;
            return color is null ? value.ToString() : $"[{color}]{value}[/]";
        }

        var dice = roll.Mode == RollMode.Normal
            ? Die(roll.Dice[0], true)
            : string.Join("|", roll.Dice.Select((d, i) => Die(d, d == roll.Natural && roll.Dice.ToList().IndexOf(roll.Natural) == i)))
              + $" [dim]({texts[roll.Mode == RollMode.Advantage ? "battle.advantage" : "battle.disadvantage"]})[/]";
        var bonus = roll.Bonus >= 0 ? $"+{roll.Bonus}" : roll.Bonus.ToString();
        return $"[dim]d20[/] {dice}{bonus} = {roll.Total}";
    }

    private static string Key(RoundEvent e) => e.Kind switch
    {
        RoundEventKind.HeroAttack => e.Roll!.Fumble ? "battle.heroFumble"
            : !e.Roll.Success ? "battle.heroMiss"
            : e.Roll.Critical ? "battle.heroCrit"
            : "battle.heroHit",
        RoundEventKind.EnemyAttack => e.Roll!.Fumble ? "battle.enemyFumble"
            : !e.Roll.Success ? "battle.enemyMiss"
            : e.Roll.Critical ? "battle.enemyCrit"
            : "battle.enemyHit",
        RoundEventKind.HeroGuards => "battle.heroGuards",
        RoundEventKind.EnemyGuards => "battle.enemyGuards",
        RoundEventKind.BothGuard => "battle.bothGuard",
        RoundEventKind.GuardOpening => "battle.guardOpening",
        RoundEventKind.HeroHeals => "battle.heroHeals",
        RoundEventKind.FleeLocked => "battle.fleeLocked",
        RoundEventKind.FleeFailed => "battle.fleeFailed",
        RoundEventKind.Fled => "battle.fled",
        _ => throw new ArgumentOutOfRangeException(nameof(e), e.Kind, null),
    };

    /// <summary>Every text key the narrator can ask for.</summary>
    public static readonly IReadOnlyList<string> Keys =
    [
        "battle.heroHit", "battle.heroCrit", "battle.heroMiss", "battle.heroFumble",
        "battle.enemyHit", "battle.enemyCrit", "battle.enemyMiss", "battle.enemyFumble",
        "battle.heroGuards", "battle.enemyGuards", "battle.bothGuard", "battle.guardOpening", "battle.heroHeals",
        "battle.fleeLocked", "battle.fleeFailed", "battle.fled", "battle.advantage", "battle.disadvantage",
    ];
}
