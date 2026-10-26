using FinalLegacy.Library.Adventures;
using FinalLegacy.Library.Enums;
using FinalLegacy.Library.Models;
using FinalLegacy.Library.Text;
using static FinalLegacy.UnitTest.TestSupport;

namespace FinalLegacy.UnitTest;

/// <summary>Whole playthroughs of the story with a scripted player.</summary>
public class AdventureTests
{
    // 0.99: the enemy always guards and every d20 is a natural 20, so the hero always wins without a scratch; every
    // random loot roll picks the rarest entry of its table and the last item of that rarity.
    private static readonly FixedDice LuckyDice = new(0.99);

    [Fact]
    public void Exploring_the_city_meets_the_mysterious_shadow()
    {
        var ui = new ScriptedUi { ClassChoice = 0, KitChoice = 1 }.Choosing(0, 0);

        var result = new Adventure(Content, Language.Portuguese, ui, LuckyDice).Run();

        Assert.Equal(AdventureOutcome.ChapterComplete, result.Outcome);
        Assert.Equal(
            new[] { "cave", "cave-chest", "skeleton-intro", "skeleton-fight", "skeleton-won", "merchant", "city-question", "city", "shadow-fight", "chapter-end" },
            result.Path);
        Assert.True(ui.TitleShown);
        Assert.Equal(2, ui.BattleResults.Count);
        Assert.Equal(3, ui.Loot.Count);
        Assert.Contains("Fim do Capítulo I", Markup.Strip(ui.Narrations[^1]));
    }

    [Fact]
    public void Not_exploring_goes_through_the_forest_to_the_golem()
    {
        var ui = new ScriptedUi { ClassChoice = 1, KitChoice = 0 }.Choosing(0, 1);

        var result = new Adventure(Content, Language.English, ui, LuckyDice).Run();

        Assert.Equal(AdventureOutcome.ChapterComplete, result.Outcome);
        Assert.Contains("forest", result.Path);
        Assert.DoesNotContain("city", result.Path);
        Assert.Equal("Stone Golem", ui.BattleScreens[^1].EnemyName);
    }

    [Fact]
    public void Refusing_to_leave_the_cave_answers_and_asks_again()
    {
        var ui = new ScriptedUi().Choosing(1, 1, 0, 0);

        new Adventure(Content, Language.Portuguese, ui, LuckyDice).Run();

        var reply = "Ahmnnn... acho que assim não vamos sair daqui tão cedo...";
        Assert.Equal(new string?[] { null, reply, reply }, ui.Notes.Take(3).Select(n => n is null ? null : Markup.Strip(n)));
        Assert.Equal(ui.ChoiceTexts[0], ui.ChoiceTexts[2]);
    }

    [Fact]
    public void Battles_drop_gold_and_random_loot()
    {
        var ui = new ScriptedUi { ClassChoice = 2, KitChoice = 1 }.Choosing(0, 0);

        var result = new Adventure(Content, Language.English, ui, LuckyDice).Run();

        // The chest: its potion and gold, plus one random roll (the rarest of its table: uncommon).
        Assert.Equal(5, ui.Loot[0].Gold);
        Assert.Equal(2, ui.Loot[0].Items.Count);
        Assert.Equal("Uncommon", ui.Loot[0].Items[1].RarityName);

        // The Shadow rolls twice on its table; the rarest entry is legendary.
        var shadowLoot = ui.Loot[2];
        Assert.Equal(25, shadowLoot.Gold);
        Assert.All(shadowLoot.Items, i => Assert.Equal("Legendary", i.RarityName));
        Assert.Equal(2, result.Hero.Bag.Count(s => s.Item.Rarity == Rarity.Legendary));
        Assert.Equal(10 + 5 + 10 + 25, result.Hero.Gold);
    }

    [Fact]
    public void Random_loot_draws_a_rarity_by_weight()
    {
        var adventure = new Adventure(Content, Language.English, new ScriptedUi(), new LoadedDice(
            Face(1, 1),   // gold: the only value
            0.25,         // roll 1: in the first half → nothing
            0.75, 0.0));  // roll 2: in the second half → rare, the first rare item
        var table = new LootTable(3, 3, [], 2, [new RarityWeight(null, 50), new RarityWeight(Rarity.Rare, 50)]);

        var (gold, items) = adventure.RollLoot(table);

        Assert.Equal(3, gold);
        Assert.Equal(Rules.OfRarity(Rarity.Rare)[0], Assert.Single(items));
    }

    [Fact]
    public void The_merchant_sells_and_the_hp_carries_over()
    {
        // At the merchant: buy the first thing on sale (a potion, 8 gold), then move on.
        var ui = new ScriptedUi { ClassChoice = 0, KitChoice = 1 }.Choosing(0, 0).InGear(0);

        var result = new Adventure(Content, Language.Portuguese, ui, LuckyDice).Run();

        var shop = ui.GearScreens[0];
        Assert.Equal("Mercador", shop.Title);
        Assert.Contains("Compraste", Markup.Strip(ui.GearScreens[1].Message!));
        Assert.Equal(10 + 5 + 10 - 8 + 25, result.Hero.Gold);
        Assert.Equal(1 + 1 + 1, result.Hero.Count("potion"));
    }

    [Fact]
    public void Losing_a_battle_ends_the_game()
    {
        var ui = new ScriptedUi().Choosing(0);
        var adventure = new Adventure(Content, Language.Portuguese, ui, new FixedDice(0.5));
        var hero = Hero("mage", "largeStaff");
        hero.Hp = 1;

        var result = adventure.Play(hero);

        Assert.Equal(AdventureOutcome.Defeated, result.Outcome);
        Assert.Equal("skeleton-fight", result.Path[^1]);
        Assert.Contains("Perdeste", Markup.Strip(Assert.Single(ui.BattleResults)));
        Assert.Contains("Fim de jogo", Markup.Strip(ui.Narrations[^1]));
    }

    [Fact]
    public void The_inventory_equips_from_the_bag_with_a_preview()
    {
        var hero = Hero("warrior", "arturiasSword");
        hero.Add(Rules.Item("ironHelm"));

        // Entries: the six slots, then the bag (the potion, the helm), then Close.
        var ui = new ScriptedUi().InGear(7);
        new Adventure(Content, Language.English, ui, LuckyDice).Inventory(hero);

        var helmEntry = ui.GearScreens[0].Entries[7];
        Assert.Equal(hero.ArmorClass, helmEntry.Preview!.ArmorClass);
        Assert.Equal("ironHelm", hero.Worn(ItemSlot.Head)!.Id);
        Assert.Contains("You equipped", Markup.Strip(ui.GearScreens[1].Message!));
    }

    [Fact]
    public void The_hero_is_built_from_the_choices()
    {
        var ui = new ScriptedUi { ClassChoice = 2, KitChoice = 1 }.Named("  Rodas  ");

        var hero = new Adventure(Content, Language.Portuguese, ui, LuckyDice).CreateHero();

        Assert.Equal("Rodas", hero.Name);
        Assert.Equal("assassin", hero.Class.Id);
        Assert.Equal("shojinBlade", hero.Worn(ItemSlot.MainHand)!.Id);
        Assert.Equal(new[] { "Guerreiro", "Mago", "Assassino" }, ui.Classes.Select(c => c.Name));
        Assert.Equal("+2 ATK  −1 DEF", ui.Kits[1].ModifiersText);
        Assert.Contains("Rodas... esse nome será relembrado...", ui.Narrations.Select(Markup.Strip));
    }

    [Fact]
    public void An_empty_or_too_long_name_is_asked_again()
    {
        // The original read the name with scanf("%s") into 30 characters: a longer one overflowed the buffer.
        var ui = new ScriptedUi().Named("", new string('x', 21), "Rodas");

        var hero = new Adventure(Content, Language.English, ui, LuckyDice).CreateHero();

        Assert.Equal("Rodas", hero.Name);
        Assert.Null(ui.NameErrors[0]);
        Assert.Contains("between 1 and 20", ui.NameErrors[1]);
        Assert.Equal(3, ui.NameErrors.Count);
    }

    [Fact]
    public void An_answer_outside_the_menu_is_a_bug_in_the_screen()
    {
        var ui = new ScriptedUi { ClassChoice = 7 };

        Assert.Throws<ArgumentOutOfRangeException>(() => new Adventure(Content, Language.English, ui, LuckyDice).CreateHero());
    }

    [Fact]
    public void The_battle_screen_shows_both_sides_and_the_dice()
    {
        var ui = new ScriptedUi { ClassChoice = 0, KitChoice = 1 }.Choosing(0, 0);

        new Adventure(Content, Language.Portuguese, ui, LuckyDice).Run();

        var first = ui.BattleScreens[0];
        Assert.Equal("Rodas", first.Hero.Name);
        Assert.Equal("Esqueleto", first.EnemyName);
        Assert.Equal(13, first.EnemyHp);
        Assert.Equal(13, first.EnemyArmorClass);
        Assert.Empty(first.Log);
        Assert.Contains(first.Actions, a => a.Label.Contains("Poção de Vida"));
        Assert.NotNull(first.Actions[^1].Hint);
        Assert.False(first.EnemyLook.IsEmpty);

        // One critical is enough for the skeleton: the end screen shows the round.
        var end = ui.EndScreens[0];
        Assert.Equal(0, end.EnemyHp);
        Assert.Equal("O Esqueleto põe-se em guarda: atacas com desvantagem.", Markup.Strip(end.Log[0]));
        Assert.StartsWith("Atacas: d20 20|20 (desvantagem)+5 = 25 — CRÍTICO!", Markup.Strip(end.Log[1]));
    }
}
