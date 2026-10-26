using FinalLegacy.Library.Enums;
using static FinalLegacy.UnitTest.TestSupport;

namespace FinalLegacy.UnitTest;

/// <summary>Equipment, the bag and the numbers they add up to.</summary>
public class HeroTests
{
    [Fact]
    public void Armour_class_adds_the_body_armour_agility_defense_and_the_other_pieces()
    {
        // Shield Warrior: 10 + Chain Shirt 3 + AGI 2 (+1, medium allows up to +2) + DEF 7 (+3) + Doran's Shield 2.
        var warrior = Hero("warrior", "doransShield");

        Assert.Equal(10 + 3 + 1 + 3 + 2, warrior.ArmorClass);
        Assert.True(warrior.Proficient);
    }

    [Fact]
    public void Heavy_armour_ignores_agility_and_untrained_classes_get_a_warning()
    {
        // Knife Assassin in Plate Armour: 10 + 6 + nothing from AGI + DEF 1 (+0).
        var assassin = Hero("assassin", "doransKnife");
        var plate = Rules.Item("plateArmor");
        assassin.Add(plate);
        assassin.Equip(plate);

        Assert.Equal(16, assassin.ArmorClass);
        Assert.False(assassin.Proficient);
        Assert.False(assassin.IsProficientWith(plate));
        Assert.Contains(assassin.Bag, s => s.Item.Id == "leatherArmor");
    }

    [Fact]
    public void Each_weapon_uses_its_own_stat()
    {
        var mage = Hero("mage", "doransRing");          // arcane: INT 8
        var assassin = Hero("assassin", "doransKnife"); // finesse: the better of ATK 7 and AGI 5
        var warrior = Hero("warrior", "arturiasSword"); // might: ATK 6

        Assert.Equal(2 + 4, mage.AttackBonus);
        Assert.Equal("1d8+4", mage.Damage.ToString());
        Assert.Equal(2 + 3, assassin.AttackBonus);
        Assert.Equal("1d6+3", assassin.Damage.ToString());
        Assert.Equal("1d10+3", warrior.Damage.ToString());
    }

    [Fact]
    public void A_two_handed_weapon_puts_the_shield_away_and_a_shield_puts_it_away_again()
    {
        var warrior = Hero("warrior", "doransShield");
        var sword = Rules.Item("arturiasSword");
        warrior.Add(sword);

        warrior.Equip(sword);

        Assert.Null(warrior.Worn(ItemSlot.OffHand));
        Assert.Contains(warrior.Bag, s => s.Item.Id == "doransShield");
        Assert.Contains(warrior.Bag, s => s.Item.Id == "ironSword");

        warrior.Equip(Rules.Item("doransShield"));

        Assert.Null(warrior.Worn(ItemSlot.MainHand));
        Assert.Contains(warrior.Bag, s => s.Item.Id == "arturiasSword");
    }

    [Fact]
    public void Potions_pile_up_and_gear_takes_its_own_place()
    {
        var hero = Hero("mage", "largeStaff");
        hero.Add(Rules.Item("potion"), 2);
        hero.Add(Rules.Item("ironHelm"), 2);

        Assert.Equal(3, hero.Count("potion"));
        Assert.Single(hero.Bag, s => s.Item.Id == "potion");
        Assert.Equal(2, hero.Bag.Count(s => s.Item.Id == "ironHelm"));
    }

    [Fact]
    public void A_clone_can_try_things_on_without_touching_the_hero()
    {
        var hero = Hero("warrior", "arturiasSword");
        var helm = Rules.Item("ironHelm");
        hero.Add(helm);

        var copy = hero.Clone();
        copy.Equip(helm);

        Assert.Null(hero.Worn(ItemSlot.Head));
        Assert.Equal(hero.ArmorClass + 1, copy.ArmorClass);
    }

    [Fact]
    public void The_starting_kits_keep_the_original_stats_gold_and_a_potion()
    {
        var hero = Hero("warrior", "doransShield");

        Assert.Equal(new[] { "doransShield", "ironSword", "chainShirt" }.Order(), hero.Equipment.Values.Select(i => i.Id).Order());
        Assert.Equal(Rules.StartGold, hero.Gold);
        Assert.Equal(1, hero.Count("potion"));
        Assert.Equal(hero.MaxHp, hero.Hp);
    }

    [Fact]
    public void Wearing_needs_the_item_in_the_bag()
    {
        var hero = Hero("mage", "largeStaff");

        Assert.Throws<InvalidOperationException>(() => hero.Equip(Rules.Item("ironHelm")));
        Assert.Throws<InvalidOperationException>(() => hero.Equip(Rules.Item("potion")));
    }
}
