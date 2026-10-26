using FinalLegacy.Library.Adventures;
using FinalLegacy.Library.Enums;
using FinalLegacy.Library.Text;
using static FinalLegacy.UnitTest.TestSupport;

namespace FinalLegacy.UnitTest;

/// <summary>Whole playthroughs of the story with a scripted player.</summary>
public class AdventureTests
{
    // 0.99: the enemy always defends and nothing ever triggers, so the hero always wins without a scratch.
    private static readonly FixedDice EasyDice = new(0.99);

    [Fact]
    public void Exploring_the_city_meets_the_mysterious_shadow()
    {
        var ui = new ScriptedUi { ClassChoice = 0, WeaponChoice = 1 }.Choosing(0, 0);

        var result = new Adventure(Content, Language.Portuguese, ui, EasyDice).Run();

        Assert.Equal(AdventureOutcome.ChapterComplete, result.Outcome);
        Assert.Equal(
            new[] { "cave", "skeleton-intro", "skeleton-fight", "skeleton-won", "city", "shadow-fight", "chapter-end" },
            result.Path);
        Assert.True(ui.TitleShown);
        Assert.Equal(2, ui.BattleResults.Count);
        Assert.Contains("Fim do Capítulo I", Markup.Strip(ui.Narrations[^1]));
    }

    [Fact]
    public void Not_exploring_goes_through_the_forest_to_the_golem()
    {
        var ui = new ScriptedUi { ClassChoice = 1, WeaponChoice = 0 }.Choosing(0, 1);

        var result = new Adventure(Content, Language.English, ui, EasyDice).Run();

        Assert.Equal(AdventureOutcome.ChapterComplete, result.Outcome);
        Assert.Contains("forest", result.Path);
        Assert.Contains("golem-fight", result.Path);
        Assert.DoesNotContain("city", result.Path);
        Assert.Equal("Stone Golem", ui.BattleScreens[^1].EnemyName);
    }

    [Fact]
    public void Refusing_to_leave_the_cave_answers_and_asks_again()
    {
        var ui = new ScriptedUi().Choosing(1, 1, 0, 0);

        new Adventure(Content, Language.Portuguese, ui, EasyDice).Run();

        Assert.Equal(new string?[] { null, "Ahmnnn... acho que assim não vamos sair daqui tão cedo...", "Ahmnnn... acho que assim não vamos sair daqui tão cedo..." },
            ui.Notes.Take(3).Select(n => n is null ? null : Markup.Strip(n)));
        Assert.Equal(ui.ChoiceTexts[0], ui.ChoiceTexts[2]);
    }

    [Fact]
    public void Losing_a_battle_ends_the_game()
    {
        // Every roll at 0: the enemy always attacks and dodges; the Staff Mage (AGI 0) never dodges back.
        var ui = new ScriptedUi { ClassChoice = 1, WeaponChoice = 1 }.Choosing(0);

        var result = new Adventure(Content, Language.Portuguese, ui, new FixedDice(0)).Run();

        Assert.Equal(AdventureOutcome.Defeated, result.Outcome);
        Assert.Equal("skeleton-fight", result.Path[^1]);
        Assert.Contains("Perdeste", Markup.Strip(Assert.Single(ui.BattleResults)));
        Assert.Contains("Fim de jogo", Markup.Strip(ui.Narrations[^1]));
    }

    [Fact]
    public void The_hero_is_built_from_the_choices()
    {
        var ui = new ScriptedUi { ClassChoice = 2, WeaponChoice = 1 }.Named("  Rodas  ");

        var hero = new Adventure(Content, Language.Portuguese, ui, EasyDice).CreateHero();

        Assert.Equal("Rodas", hero.Name);
        Assert.Equal("assassin", hero.Class.Id);
        Assert.Equal("shojinBlade", hero.Weapon.Id);
        Assert.Equal(new[] { "Guerreiro", "Mago", "Assassino" }, ui.Classes.Select(c => c.Name));
        Assert.Equal("+2 ATK  −1 DEF", ui.Weapons[1].ModifiersText);
        Assert.Contains("Rodas... esse nome será relembrado...", ui.Narrations.Select(Markup.Strip));
    }

    [Fact]
    public void An_empty_or_too_long_name_is_asked_again()
    {
        // The original read the name with scanf("%s") into 30 characters: a longer one overflowed the buffer.
        var ui = new ScriptedUi().Named("", new string('x', 21), "Rodas");

        var hero = new Adventure(Content, Language.English, ui, EasyDice).CreateHero();

        Assert.Equal("Rodas", hero.Name);
        Assert.Null(ui.NameErrors[0]);
        Assert.Contains("between 1 and 20", ui.NameErrors[1]);
        Assert.Equal(3, ui.NameErrors.Count);
    }

    [Fact]
    public void An_answer_outside_the_menu_is_a_bug_in_the_screen()
    {
        var ui = new ScriptedUi { ClassChoice = 7 };

        Assert.Throws<ArgumentOutOfRangeException>(() => new Adventure(Content, Language.English, ui, EasyDice).CreateHero());
    }

    [Fact]
    public void The_battle_screen_shows_the_hero_and_the_last_round()
    {
        var ui = new ScriptedUi { ClassChoice = 0, WeaponChoice = 1 }.Choosing(0, 0);

        new Adventure(Content, Language.Portuguese, ui, EasyDice).Run();

        var first = ui.BattleScreens[0];
        Assert.Equal("Rodas", first.HeroName);
        Assert.Equal("Guerreiro", first.ClassName);
        Assert.Equal("Espada de Artúrias", first.WeaponName);
        Assert.Equal("Esqueleto", first.EnemyName);
        Assert.Equal(100, first.HeroHp);
        Assert.Equal(50, first.EnemyHp);
        Assert.Empty(first.Log);
        Assert.False(first.CanFlee);

        var second = ui.BattleScreens[1];
        Assert.Equal(1, second.Round);
        Assert.Equal(50 - 12, second.EnemyHp);
        Assert.Equal("O Esqueleto defendeu (2 bloqueado).", Markup.Strip(second.Log[0]));
        Assert.Equal("Atacaste: 12 de dano (2 de magia).", Markup.Strip(second.Log[1]));
    }
}
