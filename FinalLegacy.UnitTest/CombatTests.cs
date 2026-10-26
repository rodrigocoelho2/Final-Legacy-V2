using FinalLegacy.Library.Combat;
using FinalLegacy.Library.Enums;
using static FinalLegacy.UnitTest.TestSupport;

namespace FinalLegacy.UnitTest;

/// <summary>
/// The d20 battle with loaded dice. Rolls are used in this order: the enemy's intent (below its aggression = it
/// attacks); the hero's d20 (two with advantage or disadvantage) and damage dice; then the enemy's d20 and damage.
/// </summary>
public class CombatTests
{
    private const double Attacks = 0.0;
    private const double Guards = 0.99;

    [Fact]
    public void A_hit_rolls_d20_plus_the_attack_bonus_against_the_armour_class()
    {
        // Warrior with the Sword of Artúrias: +5 to hit, 1d10+3. Skeleton: AC 13, 13 HP.
        var hero = Hero("warrior", "arturiasSword");
        var battle = Battle(hero, "skeleton", new LoadedDice(Attacks, Face(8), Face(5, 10), Face(1)));

        var events = battle.Play(CombatAction.Attack);

        var attack = events[0];
        Assert.Equal(RoundEventKind.HeroAttack, attack.Kind);
        Assert.Equal(8 + 5, attack.Roll!.Total);
        Assert.True(attack.Roll.Success);
        Assert.Equal(5 + 3, attack.Damage!.Total);
        Assert.Equal(13 - 8, battle.EnemyHp);

        // A natural 1 always misses.
        Assert.True(events[1].Roll!.Fumble);
        Assert.False(events[1].Roll!.Success);
        Assert.Equal(hero.MaxHp, hero.Hp);
    }

    [Fact]
    public void Below_the_armour_class_is_a_miss()
    {
        var battle = Battle(Hero("warrior", "arturiasSword"), "skeleton", new LoadedDice(Attacks, Face(7), Face(1)));

        var events = battle.Play(CombatAction.Attack);

        Assert.False(events[0].Roll!.Success);
        Assert.Null(events[0].Damage);
        Assert.Equal(13, battle.EnemyHp);
    }

    [Fact]
    public void A_natural_20_is_a_critical_that_doubles_the_dice_and_a_knocked_out_enemy_does_not_hit_back()
    {
        var battle = Battle(Hero("warrior", "arturiasSword"), "skeleton", new LoadedDice(Attacks, Face(20), Face(4, 10), Face(6, 10)));

        var events = battle.Play(CombatAction.Attack);

        var attack = Assert.Single(events);
        Assert.True(attack.Roll!.Critical);
        Assert.Equal(2, attack.Damage!.Rolls.Count);
        Assert.Equal(4 + 6 + 3, attack.Damage.Total);
        Assert.Equal(BattleOutcome.Won, battle.Outcome);
    }

    [Fact]
    public void Keen_blades_are_critical_on_19()
    {
        // Assassin with the Blade of Shojin (critical on 19–20): 1d8 doubled + 3.
        var battle = Battle(Hero("assassin", "shojinBlade"), "shadow", new LoadedDice(Attacks, Face(19), Face(1, 8), Face(1, 8), Face(1)));

        var events = battle.Play(CombatAction.Attack);

        Assert.True(events[0].Roll!.Critical);
        Assert.Equal(new[] { 1, 1 }, events[0].Damage!.Rolls);
        Assert.Equal(1 + 1 + 3, events[0].Damage!.Total);
    }

    [Fact]
    public void A_guarding_enemy_gives_the_hero_disadvantage()
    {
        var battle = Battle(Hero("warrior", "arturiasSword"), "skeleton", new LoadedDice(Guards, Face(18), Face(3)));

        var events = battle.Play(CombatAction.Attack);

        Assert.Equal(new[] { RoundEventKind.EnemyGuards, RoundEventKind.HeroAttack }, events.Select(e => e.Kind));
        Assert.Equal(new[] { 18, 3 }, events[1].Roll!.Dice);
        Assert.Equal(3, events[1].Roll!.Natural);
        Assert.False(events[1].Roll!.Success);
    }

    [Fact]
    public void Defending_gives_the_enemy_disadvantage_and_a_block_opens_it_up()
    {
        var hero = Hero("warrior", "doransShield");
        var battle = Battle(hero, "skeleton", new LoadedDice(
            Attacks, Face(19), Face(2),          // round 1: the skeleton attacks with disadvantage and keeps the 2
            Attacks, Face(4), Face(15), Face(5, 8), Face(1)));  // round 2: advantage, keeps the 15

        var first = battle.Play(CombatAction.Defend);

        Assert.Equal(new[] { RoundEventKind.HeroGuards, RoundEventKind.EnemyAttack, RoundEventKind.GuardOpening }, first.Select(e => e.Kind));
        Assert.Equal(RollMode.Disadvantage, first[1].Roll!.Mode);
        Assert.True(battle.HeroHasOpening);

        var second = battle.Play(CombatAction.Attack);

        Assert.Equal(RollMode.Advantage, second[0].Roll!.Mode);
        Assert.Equal(15, second[0].Roll!.Natural);
        Assert.True(second[0].Roll!.Success);
        Assert.False(battle.HeroHasOpening);
    }

    [Fact]
    public void Armour_the_class_is_not_trained_in_gives_disadvantage()
    {
        var mage = Hero("mage", "largeStaff");
        mage.Add(Rules.Item("plateArmor"));
        mage.Equip(Rules.Item("plateArmor"));
        Assert.False(mage.Proficient);

        var battle = Battle(mage, "skeleton", new LoadedDice(Attacks, Face(17), Face(2), Face(1)));
        var events = battle.Play(CombatAction.Attack);

        Assert.Equal(RollMode.Disadvantage, events[0].Roll!.Mode);
        Assert.False(events[0].Roll!.Success);
    }

    [Fact]
    public void A_potion_heals_but_takes_the_round()
    {
        var hero = Hero("warrior", "doransShield");
        hero.Hp = 10;
        var potion = Rules.Item("potion");
        var battle = Battle(hero, "skeleton", new LoadedDice(Attacks, Face(3, 4), Face(4, 4), Face(1)));

        var events = battle.Play(CombatAction.UseItem, potion);

        Assert.Equal(RoundEventKind.HeroHeals, events[0].Kind);
        Assert.Equal(10 + 3 + 4 + 2, hero.Hp);
        Assert.Equal(0, hero.Count("potion"));
        Assert.Equal(RoundEventKind.EnemyAttack, events[1].Kind);
        Assert.Throws<InvalidOperationException>(() => battle.Play(CombatAction.UseItem, potion));
    }

    [Fact]
    public void Trying_to_flee_a_battle_that_forbids_it_costs_nothing()
    {
        // The original printed "Não podes fugir" even when you could, and ran a round anyway. No dice are rolled now.
        var battle = Battle(Hero("warrior", "doransShield"), "skeleton", new LoadedDice());

        Assert.Equal(RoundEventKind.FleeLocked, Assert.Single(battle.Play(CombatAction.Flee)).Kind);
        Assert.Equal(0, battle.Round);
        Assert.Equal(BattleOutcome.Ongoing, battle.Outcome);
    }

    [Fact]
    public void Fleeing_is_d20_plus_agility_against_the_difficulty()
    {
        // Assassin AGI 5: +2. 10 + 2 = 12 against 12 gets away.
        var battle = Battle(Hero("assassin", "doransKnife"), "skeleton", new LoadedDice(Attacks, Face(10)), canFlee: true);

        var events = battle.Play(CombatAction.Flee);

        Assert.Equal(RoundEventKind.Fled, Assert.Single(events).Kind);
        Assert.Equal(BattleOutcome.Fled, battle.Outcome);
    }

    [Fact]
    public void A_failed_escape_gives_the_enemy_a_hit_even_if_it_meant_to_guard()
    {
        var battle = Battle(Hero("assassin", "doransKnife"), "skeleton", new LoadedDice(Guards, Face(3), Face(1)), canFlee: true);

        var events = battle.Play(CombatAction.Flee);

        Assert.Equal(new[] { RoundEventKind.FleeFailed, RoundEventKind.EnemyAttack }, events.Select(e => e.Kind));
    }

    [Fact]
    public void The_hero_loses_at_zero_hp_and_hp_carries_over()
    {
        var hero = Hero("mage", "largeStaff");
        hero.Hp = 3;
        var battle = Battle(hero, "skeleton", new LoadedDice(Attacks, Face(1), Face(15), Face(4, 6)));

        battle.Play(CombatAction.Attack);

        Assert.Equal(BattleOutcome.Lost, battle.Outcome);
        Assert.Equal(0, hero.Hp);
        Assert.Throws<InvalidOperationException>(() => battle.Play(CombatAction.Attack));
    }

    [Theory]
    [InlineData("1d8", 1, 8, 0)]
    [InlineData("2d6+2", 2, 6, 2)]
    [InlineData("1d4-1", 1, 4, -1)]
    public void Dice_read_dnd_notation(string text, int count, int sides, int bonus)
    {
        var dice = Dice.Parse(text);

        Assert.Equal(new Dice(count, sides, bonus), dice);
        Assert.Equal(text, dice.ToString());
    }
}
