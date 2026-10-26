using FinalLegacy.Library.Enums;
using FinalLegacy.Library.Models;

namespace FinalLegacy.Library.Combat;

/// <summary>
/// One fight, round by round. The rules are the original's (attack, defend or flee against an enemy that randomly
/// attacks or defends), with agility and intelligence now counting too:
/// <list type="bullet">
///   <item>Attacking deals your ATK minus half the enemy's DEF (its armour); all of its DEF if it defends.</item>
///   <item>Intelligence adds INT ÷ 2 of magic damage that no defence stops (the Mage's strength).</item>
///   <item>Agility gives a chance of a critical hit (double ATK) and of dodging a hit when not defending.</item>
///   <item>Your armour takes half your DEF off every hit; defending takes all of it.</item>
///   <item>Blocking a hit sets up a counter-attack: the next hit that lands is a critical that ignores armour.</item>
///   <item>Fleeing, when the battle allows it, works more often the more agile you are; failing costs a free hit.</item>
/// </list>
/// The hero acts first, so an enemy that drops to 0 HP doesn't hit back.
/// The dice are rolled in a fixed order so a test can load them: the enemy's intent; then the hero's attack (the
/// enemy's dodge unless it defends, the critical unless a counter is ready); then the enemy's attack (the hero's dodge
/// unless defending). Fleeing rolls the escape first, and the hero's dodge if the escape fails.
/// </summary>
public sealed class Battle
{
    private const double MaxChance = 0.95;

    private readonly CombatSettings _settings;
    private readonly IRandomSource _random;
    private bool _counterReady;

    public Battle(Hero hero, EnemyType enemy, bool canFlee, CombatSettings settings, IRandomSource random)
    {
        Hero = hero;
        Enemy = enemy;
        CanFlee = canFlee;
        _settings = settings;
        _random = random;
        HeroHp = hero.MaxHp;
        EnemyHp = enemy.MaxHp;
    }

    public Hero Hero { get; }
    public EnemyType Enemy { get; }
    public bool CanFlee { get; }
    public int HeroHp { get; private set; }
    public int EnemyHp { get; private set; }

    /// <summary>Rounds played so far (a refused flee doesn't count).</summary>
    public int Round { get; private set; }

    public BattleOutcome Outcome { get; private set; } = BattleOutcome.Ongoing;

    /// <summary>The hero blocked a hit, so their next attack is a guaranteed critical (a counter-attack).</summary>
    public bool CounterReady => _counterReady;

    /// <summary>Plays one round with the hero's action and tells what happened.</summary>
    public IReadOnlyList<RoundEvent> Play(CombatAction action)
    {
        if (Outcome != BattleOutcome.Ongoing)
        {
            throw new InvalidOperationException($"The battle is over ({Outcome}).");
        }

        var events = new List<RoundEvent>();

        if (action == CombatAction.Flee)
        {
            if (!CanFlee)
            {
                // The original printed this and let you choose again; the enemy doesn't get a free turn.
                events.Add(new RoundEvent(RoundEventKind.FleeLocked));
                return events;
            }

            Round++;
            if (Roll(FleeChance))
            {
                Outcome = BattleOutcome.Fled;
                events.Add(new RoundEvent(RoundEventKind.Fled));
                return events;
            }

            events.Add(new RoundEvent(RoundEventKind.FleeFailed));
            EnemyAttacks(heroDefending: false, events);
            Settle();
            return events;
        }

        Round++;
        var enemyAttacks = Roll(Enemy.Aggression);

        if (action == CombatAction.Attack)
        {
            HeroAttacks(enemyDefending: !enemyAttacks, events);
        }
        else if (!enemyAttacks)
        {
            events.Add(new RoundEvent(RoundEventKind.BothDefend));
        }

        if (enemyAttacks && EnemyHp > 0)
        {
            EnemyAttacks(heroDefending: action == CombatAction.Defend, events);
        }

        Settle();
        return events;
    }

    /// <summary>Chance of getting away: the base chance plus a bit per point of agility.</summary>
    public double FleeChance =>
        Math.Min(MaxChance, _settings.FleeBaseChance + Hero.Stats.Agility * _settings.FleeChancePerAgility);

    public double HeroCritChance => Math.Min(MaxChance, Hero.Stats.Agility * _settings.CritChancePerAgility);

    public double HeroDodgeChance => Math.Min(MaxChance, Hero.Stats.Agility * _settings.DodgeChancePerAgility);

    public double EnemyDodgeChance => Math.Min(MaxChance, Enemy.Stats.Agility * _settings.DodgeChancePerAgility);

    private void HeroAttacks(bool enemyDefending, List<RoundEvent> events)
    {
        var stats = Hero.Stats;

        if (!enemyDefending && Roll(EnemyDodgeChance))
        {
            events.Add(new RoundEvent(RoundEventKind.EnemyDodged));
            return;
        }

        var counter = _counterReady;
        _counterReady = false;
        var critical = counter || Roll(HeroCritChance);
        var physical = stats.Attack * _settings.AttackMultiplier * (critical ? 2 : 1);
        var armour = counter ? 0 : enemyDefending ? Enemy.Stats.Defense : Enemy.Stats.Defense / 2;
        var blocked = Math.Min(physical, armour);
        var magic = stats.Intelligence / _settings.MagicDivisor;
        var damage = Math.Max(_settings.MinimumHit, physical - blocked + magic);

        if (enemyDefending && !counter)
        {
            events.Add(new RoundEvent(RoundEventKind.EnemyDefended, Blocked: blocked));
        }

        EnemyHp = Math.Max(0, EnemyHp - damage);
        var kind = counter ? RoundEventKind.HeroCounter : critical ? RoundEventKind.HeroCrit : RoundEventKind.HeroHit;
        events.Add(new RoundEvent(kind, damage, blocked, magic));
    }

    private void EnemyAttacks(bool heroDefending, List<RoundEvent> events)
    {
        var attack = Enemy.Stats.Attack;

        if (heroDefending)
        {
            var blocked = Math.Min(attack, Hero.Stats.Defense);
            var damage = attack - blocked;
            HeroHp = Math.Max(0, HeroHp - damage);
            _counterReady = true;
            events.Add(new RoundEvent(RoundEventKind.HeroBlocked, damage, blocked));
            return;
        }

        if (Roll(HeroDodgeChance))
        {
            events.Add(new RoundEvent(RoundEventKind.HeroDodged));
            return;
        }

        var armour = Math.Min(attack, Hero.Stats.Defense / 2);
        HeroHp = Math.Max(0, HeroHp - (attack - armour));
        events.Add(new RoundEvent(RoundEventKind.EnemyHit, attack - armour, armour));
    }

    private void Settle()
    {
        if (HeroHp == 0) Outcome = BattleOutcome.Lost;
        else if (EnemyHp == 0) Outcome = BattleOutcome.Won;
    }

    private bool Roll(double chance) => _random.NextDouble() < chance;
}
