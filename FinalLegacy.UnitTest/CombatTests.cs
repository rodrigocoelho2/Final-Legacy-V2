using FinalLegacy.Library.Enums;
using static FinalLegacy.UnitTest.TestSupport;

namespace FinalLegacy.UnitTest;

/// <summary>
/// The battle rules with loaded dice. Rolls are consumed in this order: the enemy's intent (below its aggression =
/// it attacks), the enemy's dodge (only if it doesn't defend), the hero's critical (not when a counter is ready),
/// then the hero's dodge (only if the hero doesn't defend). 0.99 never triggers anything.
/// </summary>
public class CombatTests
{
    private const double Attacks = 0.0;
    private const double Defends = 0.99;
    private const double No = 0.99;
    private const double Yes = 0.0;

    [Fact]
    public void Attack_deals_double_attack_minus_half_armour_plus_magic()
    {
        // Warrior with the Sword of Artúrias: ATK 6, DEF 4, AGI 2, INT 2. Skeleton: 50 HP, ATK 12, DEF 2.
        var battle = Battle(Hero("warrior", "arturiasSword"), "skeleton", new LoadedDice(Attacks, No, No, No));

        var events = battle.Play(CombatAction.Attack);

        Assert.Equal(RoundEventKind.HeroHit, events[0].Kind);
        Assert.Equal(6 * 2 - 2 / 2 + 2, events[0].Damage);
        Assert.Equal(2, events[0].Magic);
        Assert.Equal(50 - 13, battle.EnemyHp);

        Assert.Equal(RoundEventKind.EnemyHit, events[1].Kind);
        Assert.Equal(12 - 4 / 2, events[1].Damage);
        Assert.Equal(2, events[1].Blocked);
        Assert.Equal(100 - 10, battle.HeroHp);
        Assert.Equal(1, battle.Round);
    }

    [Fact]
    public void A_defending_enemy_takes_its_whole_defense_off()
    {
        var battle = Battle(Hero("warrior", "arturiasSword"), "skeleton", new LoadedDice(Defends, No));

        var events = battle.Play(CombatAction.Attack);

        Assert.Equal(2, events.Count);
        Assert.Equal(RoundEventKind.EnemyDefended, events[0].Kind);
        Assert.Equal(2, events[0].Blocked);
        Assert.Equal(12 - 2 + 2, events[1].Damage);
        Assert.Equal(100, battle.HeroHp);
    }

    [Fact]
    public void Magic_goes_through_any_defense()
    {
        // Mage with Doran's Ring: ATK 2 (4 after doubling), INT 8. The Golem defends with DEF 6: 4 - 4 + 8.
        var battle = Battle(Hero("mage", "doransRing"), "golem", new LoadedDice(Defends, No));

        var events = battle.Play(CombatAction.Attack);

        Assert.Equal(0 + 8, events[1].Damage);
        Assert.Equal(4, events[1].Blocked);
        Assert.Equal(8, events[1].Magic);
    }

    [Fact]
    public void A_critical_hit_doubles_the_physical_damage()
    {
        // Assassin with the Blade of Shojin: ATK 7, AGI 5 (20 % critical). Skeleton defends: 28 - 2 + 2.
        var battle = Battle(Hero("assassin", "shojinBlade"), "skeleton", new LoadedDice(Defends, 0.19));

        var events = battle.Play(CombatAction.Attack);

        Assert.Equal(RoundEventKind.HeroCrit, events[1].Kind);
        Assert.Equal(28, events[1].Damage);
    }

    [Fact]
    public void Defending_takes_the_whole_defense_off_and_sets_up_a_counter_attack()
    {
        // Warrior with Doran's Shield: ATK 3, DEF 7. Skeleton attacks for 12: 12 - 7.
        var battle = Battle(Hero("warrior", "doransShield"), "skeleton", new LoadedDice(Attacks, Defends));

        var events = battle.Play(CombatAction.Defend);

        Assert.Equal(RoundEventKind.HeroBlocked, Assert.Single(events).Kind);
        Assert.Equal(5, events[0].Damage);
        Assert.Equal(7, events[0].Blocked);
        Assert.Equal(95, battle.HeroHp);
        Assert.True(battle.CounterReady);

        // The counter is a critical that ignores the enemy's guard (no critical roll): 3 × 2 × 2 + INT 2.
        var counter = battle.Play(CombatAction.Attack);

        Assert.Equal(RoundEventKind.HeroCounter, Assert.Single(counter).Kind);
        Assert.Equal(14, counter[0].Damage);
        Assert.False(battle.CounterReady);
    }

    [Fact]
    public void When_both_defend_nobody_gets_hurt()
    {
        var battle = Battle(Hero("mage", "largeStaff"), "skeleton", new LoadedDice(Defends));

        var events = battle.Play(CombatAction.Defend);

        Assert.Equal(RoundEventKind.BothDefend, Assert.Single(events).Kind);
        Assert.Equal(100, battle.HeroHp);
        Assert.Equal(50, battle.EnemyHp);
        Assert.False(battle.CounterReady);
    }

    [Fact]
    public void Agility_lets_both_sides_dodge()
    {
        // Assassin AGI 5: 15 % to dodge. The Shadow has AGI 5 too.
        var battle = Battle(Hero("assassin", "doransKnife"), "shadow", new LoadedDice(Attacks, 0.14, 0.14));

        var events = battle.Play(CombatAction.Attack);

        Assert.Equal(new[] { RoundEventKind.EnemyDodged, RoundEventKind.HeroDodged }, events.Select(e => e.Kind));
        Assert.Equal(100, battle.HeroHp);
        Assert.Equal(60, battle.EnemyHp);
    }

    [Fact]
    public void Trying_to_flee_a_battle_that_forbids_it_costs_nothing()
    {
        // The original printed "Não podes fugir" even when you could, and ran a round anyway. No dice are rolled now.
        var dice = new LoadedDice();
        var battle = Battle(Hero("warrior", "doransShield"), "skeleton", dice);

        var events = battle.Play(CombatAction.Flee);

        Assert.Equal(RoundEventKind.FleeLocked, Assert.Single(events).Kind);
        Assert.Equal(0, battle.Round);
        Assert.Equal(BattleOutcome.Ongoing, battle.Outcome);
    }

    [Fact]
    public void Agility_makes_fleeing_easier()
    {
        // 40 % + 10 % per AGI: the Assassin (AGI 5) gets away 90 % of the time.
        var battle = Battle(Hero("assassin", "doransKnife"), "skeleton", new LoadedDice(0.89), canFlee: true);

        Assert.Equal(0.9, battle.FleeChance, 3);
        Assert.Equal(RoundEventKind.Fled, Assert.Single(battle.Play(CombatAction.Flee)).Kind);
        Assert.Equal(BattleOutcome.Fled, battle.Outcome);
    }

    [Fact]
    public void A_failed_escape_gives_the_enemy_a_free_hit()
    {
        var battle = Battle(Hero("mage", "largeStaff"), "skeleton", new LoadedDice(No, No), canFlee: true);

        var events = battle.Play(CombatAction.Flee);

        Assert.Equal(new[] { RoundEventKind.FleeFailed, RoundEventKind.EnemyHit }, events.Select(e => e.Kind));
        Assert.Equal(100 - (12 - 1), battle.HeroHp);
    }

    [Fact]
    public void An_enemy_knocked_out_does_not_hit_back_and_the_battle_is_won()
    {
        var battle = Battle(Hero("assassin", "shojinBlade"), "skeleton", new FixedDice(Defends));

        while (battle.Outcome == BattleOutcome.Ongoing)
        {
            battle.Play(CombatAction.Attack);
        }

        Assert.Equal(BattleOutcome.Won, battle.Outcome);
        Assert.Equal(0, battle.EnemyHp);
        Assert.Equal(100, battle.HeroHp);
        Assert.Throws<InvalidOperationException>(() => battle.Play(CombatAction.Attack));
    }

    [Fact]
    public void The_hero_loses_at_zero_hp()
    {
        // Every roll at 0: the enemy always attacks and always dodges; the Staff Mage (AGI 0) never dodges.
        var battle = Battle(Hero("mage", "largeStaff"), "skeleton", new FixedDice(Yes));

        while (battle.Outcome == BattleOutcome.Ongoing)
        {
            battle.Play(CombatAction.Attack);
        }

        Assert.Equal(BattleOutcome.Lost, battle.Outcome);
        Assert.Equal(0, battle.HeroHp);
        Assert.Equal(50, battle.EnemyHp);
    }
}
