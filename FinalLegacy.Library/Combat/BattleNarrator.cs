using FinalLegacy.Library.Enums;
using FinalLegacy.Library.Localization;
using FinalLegacy.Library.Models;

namespace FinalLegacy.Library.Combat;

/// <summary>Turns what happened in a round into the lines shown under the HP bars, in the chosen language.</summary>
public sealed class BattleNarrator(Localizer texts)
{
    public IReadOnlyList<string> Describe(EnemyType enemy, IEnumerable<RoundEvent> events)
    {
        var lines = new List<string>();
        foreach (var e in events)
        {
            lines.Add(Describe(enemy, e));
            if (e.Kind == RoundEventKind.HeroBlocked)
            {
                lines.Add(texts["battle.counterReady"]);
            }
        }

        return lines;
    }

    public string Describe(EnemyType enemy, RoundEvent e)
    {
        var the = texts[$"enemy.{enemy.Id}.the"];
        (string, object?)[] values =
        [
            ("enemy", texts[$"enemy.{enemy.Id}"]),
            ("the", the),
            ("The", Localizer.Capitalize(the)),
            ("of", texts[$"enemy.{enemy.Id}.of"]),
            ("damage", e.Damage),
            ("blocked", e.Blocked),
            ("magic", e.Magic),
        ];

        var line = texts.Format(Key(e.Kind), values);
        if (e.Kind is RoundEventKind.HeroHit or RoundEventKind.HeroCrit or RoundEventKind.HeroCounter && e.Magic > 0)
        {
            // "Atacaste: 6 de dano." → "Atacaste: 6 de dano (4 de magia)."
            var magic = texts.Format("battle.magicPart", values);
            line = line.EndsWith('.') ? line[..^1] + magic + "." : line + magic;
        }
        else if (e.Kind == RoundEventKind.EnemyHit && e.Blocked > 0)
        {
            // "O Esqueleto atacou: 4 de dano." → "O Esqueleto atacou: 4 de dano (3 travado pela armadura)."
            var armour = texts.Format("battle.armorPart", values);
            line = line.EndsWith('.') ? line[..^1] + armour + "." : line + armour;
        }

        return line;
    }

    private static string Key(RoundEventKind kind) => kind switch
    {
        RoundEventKind.HeroHit => "battle.heroHit",
        RoundEventKind.HeroCrit => "battle.heroCrit",
        RoundEventKind.HeroCounter => "battle.heroCounter",
        RoundEventKind.EnemyDodged => "battle.enemyDodged",
        RoundEventKind.EnemyDefended => "battle.enemyDefended",
        RoundEventKind.EnemyHit => "battle.enemyHit",
        RoundEventKind.HeroBlocked => "battle.heroBlocked",
        RoundEventKind.HeroDodged => "battle.heroDodged",
        RoundEventKind.BothDefend => "battle.bothDefend",
        RoundEventKind.FleeLocked => "battle.fleeLocked",
        RoundEventKind.FleeFailed => "battle.fleeFailed",
        RoundEventKind.Fled => "battle.fled",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };
}
