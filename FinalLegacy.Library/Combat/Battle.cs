using FinalLegacy.Library.Enums;
using FinalLegacy.Library.Items;
using FinalLegacy.Library.Models;

namespace FinalLegacy.Library.Combat;

/// <summary>Something that happened in a round, with the dice behind it.</summary>
public sealed record RoundEvent(
    RoundEventKind Kind,
    D20Roll? Roll = null,
    DamageRoll? Damage = null,
    Item? Item = null);

/// <summary>
/// One fight, D&amp;D style, keeping the original's choices (attack, defend, flee — plus drinking a potion) and its
/// enemy that randomly attacks or guards.
/// <list type="bullet">
///   <item>Every round the enemy first decides: attack (its aggression) or guard.</item>
///   <item>Attacking rolls d20 + attack bonus against the enemy's CA, with disadvantage if it guards (or if the hero
///   wears armour they aren't trained in). A natural 20 is a critical (double dice), a natural 1 always misses.</item>
///   <item>Defending gives the enemy disadvantage; if it misses, the hero's guard opened it up and their next attack
///   has advantage.</item>
///   <item>A potion heals but takes the round.</item>
///   <item>Fleeing, when allowed, is d20 + AGI modifier against the flee difficulty; failing gives the enemy a hit.</item>
/// </list>
/// The hero acts first, so a knocked-out enemy doesn't hit back. HP is the hero's own and carries over.
/// </summary>
public sealed class Battle
{
    private readonly CombatSettings _settings;
    private readonly IRandomSource _random;

    public Battle(Hero hero, EnemyType enemy, bool canFlee, CombatSettings settings, IRandomSource random)
    {
        Hero = hero;
        Enemy = enemy;
        CanFlee = canFlee;
        _settings = settings;
        _random = random;
        EnemyHp = enemy.MaxHp;
    }

    public Hero Hero { get; }
    public EnemyType Enemy { get; }
    public bool CanFlee { get; }
    public int EnemyHp { get; private set; }
    public int Round { get; private set; }
    public BattleOutcome Outcome { get; private set; } = BattleOutcome.Ongoing;

    /// <summary>The hero blocked a hit while defending: their next attack has advantage.</summary>
    public bool HeroHasOpening { get; private set; }

    /// <summary>Plays a round. <paramref name="potion"/> is the potion to drink for <see cref="CombatAction.UseItem"/>.</summary>
    public IReadOnlyList<RoundEvent> Play(CombatAction action, Item? potion = null)
    {
        if (Outcome != BattleOutcome.Ongoing) throw new InvalidOperationException($"The battle is over ({Outcome}).");

        var events = new List<RoundEvent>();
        if (action == CombatAction.Flee && !CanFlee)
        {
            // The original said this and let you choose again; no round goes by.
            events.Add(new RoundEvent(RoundEventKind.FleeLocked));
            return events;
        }

        if (action == CombatAction.UseItem && (potion?.Consumable is null || Hero.Count(potion.Id) == 0))
        {
            throw new InvalidOperationException("Drinking needs a potion the hero has.");
        }

        Round++;
        var enemyAttacks = Roll() < Enemy.Aggression;
        var defending = false;
        if (!enemyAttacks && action is CombatAction.Attack or CombatAction.UseItem)
        {
            events.Add(new RoundEvent(RoundEventKind.EnemyGuards));
        }

        switch (action)
        {
            case CombatAction.Attack:
                HeroAttacks(enemyGuards: !enemyAttacks, events);
                break;
            case CombatAction.Defend:
                defending = true;
                events.Add(new RoundEvent(enemyAttacks ? RoundEventKind.HeroGuards : RoundEventKind.BothGuard));
                break;
            case CombatAction.UseItem:
                events.Add(new RoundEvent(RoundEventKind.HeroHeals, Damage: Hero.Drink(potion!, _random), Item: potion));
                break;
            case CombatAction.Flee:
                var escape = D20Roll.Roll(_random, RollMode.Normal, Hero.Mod(Hero.Stats.Agility), _settings.FleeDifficulty);
                if (escape.Success)
                {
                    Outcome = BattleOutcome.Fled;
                    events.Add(new RoundEvent(RoundEventKind.Fled, escape));
                    return events;
                }

                events.Add(new RoundEvent(RoundEventKind.FleeFailed, escape));
                enemyAttacks = true;
                break;
        }

        if (enemyAttacks && EnemyHp > 0)
        {
            EnemyAttacks(defending, events);
        }

        if (Hero.Hp == 0) Outcome = BattleOutcome.Lost;
        else if (EnemyHp == 0) Outcome = BattleOutcome.Won;
        return events;
    }

    private void HeroAttacks(bool enemyGuards, List<RoundEvent> events)
    {
        var mode = D20Roll.ModeOf(HeroHasOpening, enemyGuards || !Hero.Proficient);
        HeroHasOpening = false;

        var roll = D20Roll.Roll(_random, mode, Hero.AttackBonus, Enemy.ArmorClass, Hero.Weapon.CritFrom);
        DamageRoll? damage = null;
        if (roll.Success)
        {
            damage = Hero.Damage.Roll(_random, roll.Critical);
            EnemyHp = Math.Max(0, EnemyHp - damage.Total);
        }

        events.Add(new RoundEvent(RoundEventKind.HeroAttack, roll, damage));
    }

    private void EnemyAttacks(bool heroDefending, List<RoundEvent> events)
    {
        var mode = heroDefending ? RollMode.Disadvantage : RollMode.Normal;
        var roll = D20Roll.Roll(_random, mode, Enemy.AttackBonus, Hero.ArmorClass);
        DamageRoll? damage = null;
        if (roll.Success)
        {
            damage = Enemy.Damage.Roll(_random, roll.Critical);
            Hero.Hp = Math.Max(0, Hero.Hp - damage.Total);
        }

        events.Add(new RoundEvent(RoundEventKind.EnemyAttack, roll, damage));

        if (heroDefending && !roll.Success && Hero.Hp > 0)
        {
            HeroHasOpening = true;
            events.Add(new RoundEvent(RoundEventKind.GuardOpening));
        }
    }

    private double Roll() => _random.NextDouble();
}
