using FinalLegacy.Library.Combat;
using FinalLegacy.Library.Content;
using FinalLegacy.Library.Enums;
using FinalLegacy.Library.Localization;
using FinalLegacy.Library.Models;
using FinalLegacy.Library.Story;
using FinalLegacy.Library.Text;

namespace FinalLegacy.Library.Adventures;

/// <summary>How a run ended, the hero that played it and the scenes it went through.</summary>
public sealed record AdventureResult(AdventureOutcome Outcome, Hero Hero, IReadOnlyList<string> Path);

/// <summary>
/// One playthrough: builds the hero (class, weapon, name), then walks the story scene by scene, asking the screen
/// for every choice and running every battle.
/// </summary>
public sealed class Adventure(GameContent content, Language language, IGameUi ui, IRandomSource random)
{
    private readonly Localizer _texts = content.Texts[language];
    private readonly GameRules _rules = content.Rules;

    public AdventureResult Run()
    {
        ui.ShowTitle();
        var hero = CreateHero();
        return Play(hero);
    }

    /// <summary>Class, weapon and name, in the original's order.</summary>
    public Hero CreateHero()
    {
        var classes = _rules.Classes;
        var classIndex = ui.ChooseClass(classes.Select(c => new ClassOption(
            _texts[$"class.{c.Id}.name"],
            c.Color,
            _texts[$"class.{c.Id}.blurb"],
            c.BaseStats.AtLeastZero())).ToList());
        var heroClass = classes[Checked(classIndex, classes.Count)];

        var weapons = _rules.WeaponsOf(heroClass);
        var weaponIndex = ui.ChooseWeapon(
            _texts[$"class.{heroClass.Id}.weapons"],
            weapons.Select(w => new WeaponOption(
                _texts[$"weapon.{w.Id}"],
                ModifiersText(w.Modifiers),
                (heroClass.BaseStats + w.Modifiers).AtLeastZero())).ToList());
        var weapon = weapons[Checked(weaponIndex, weapons.Count)];

        ui.Narrate(_texts.Format(
            "ui.classSummary",
            ("class", Markup.Colorize(_texts[$"class.{heroClass.Id}.name"], heroClass.Color)),
            ("weapon", Markup.Colorize(_texts[$"weapon.{weapon.Id}"], "cyan"))));

        var name = AskName();
        var hero = new Hero(name, heroClass, weapon, _rules.HeroMaxHp);
        ui.Narrate(Fill(_texts["ui.nameRemembered"], hero));
        return hero;
    }

    /// <summary>Walks the story from its start scene with an existing hero.</summary>
    public AdventureResult Play(Hero hero)
    {
        var path = new List<string>();
        var scene = content.Story[content.Story.Start];

        while (true)
        {
            path.Add(scene.Id);
            switch (scene.Kind)
            {
                case SceneKind.Narration:
                    ui.Narrate(Text(scene, hero));
                    scene = content.Story[scene.Goto!];
                    break;

                case SceneKind.Choice:
                    scene = content.Story[Choose(scene, hero)];
                    break;

                case SceneKind.Battle:
                    var outcome = Fight(hero, scene.Battle!);
                    if (outcome == BattleOutcome.Lost)
                    {
                        ui.Narrate(_texts["game.over"]);
                        return new AdventureResult(AdventureOutcome.Defeated, hero, path);
                    }

                    scene = content.Story[outcome == BattleOutcome.Fled ? scene.Flee! : scene.Win!];
                    break;

                case SceneKind.End:
                    ui.Narrate(Text(scene, hero));
                    return new AdventureResult(AdventureOutcome.ChapterComplete, hero, path);
            }
        }
    }

    /// <summary>Runs a battle on the screen until someone wins or the hero gets away.</summary>
    public BattleOutcome Fight(Hero hero, BattleSpec spec)
    {
        var enemy = _rules.Enemy(spec.EnemyId);
        var battle = new Battle(hero, enemy, spec.CanFlee, _rules.Combat, random);
        var narrator = new BattleNarrator(_texts);
        IReadOnlyList<string> log = [];

        while (battle.Outcome == BattleOutcome.Ongoing)
        {
            var action = ui.ChooseAction(Screen(battle, log));
            log = narrator.Describe(enemy, battle.Play(action));
        }

        var result = battle.Outcome switch
        {
            BattleOutcome.Won => _texts["battle.won"],
            BattleOutcome.Lost => _texts["battle.lost"],
            _ => _texts["battle.fled"],
        };
        ui.ShowBattleEnd(Screen(battle, log), result);
        return battle.Outcome;
    }

    /// <summary>"+2 DEF  −1 ATK": the gains first, then the losses.</summary>
    public string ModifiersText(Stats modifiers) => string.Join("  ", modifiers.Entries()
        .Where(e => e.Value != 0)
        .OrderByDescending(e => e.Value > 0)
        .Select(e => $"{(e.Value > 0 ? "+" : "−")}{Math.Abs(e.Value)} {_texts[e.Key]}"));

    /// <summary>A name is trimmed and must be 1 to NameMaxLength characters long.</summary>
    public static bool IsValidName(string? name, int maxLength) =>
        !string.IsNullOrWhiteSpace(name) && name.Trim().Length <= maxLength;

    private string AskName()
    {
        string? error = null;
        while (true)
        {
            var answer = ui.AskName(_texts["ui.askName"], error);
            if (IsValidName(answer, _rules.NameMaxLength))
            {
                return answer.Trim();
            }

            error = _texts.Format("ui.nameInvalid", ("max", _rules.NameMaxLength));
        }
    }

    private string Choose(Scene scene, Hero hero)
    {
        var text = Text(scene, hero);
        var labels = scene.Choices.Select(c => _texts[c.LabelKey]).ToList();
        string? note = null;

        while (true)
        {
            var choice = scene.Choices[Checked(ui.Choose(text, labels, note), labels.Count)];
            if (choice.Goto is not null)
            {
                return choice.Goto;
            }

            note = Fill(_texts[choice.ReplyKey!], hero);
        }
    }

    private BattleScreen Screen(Battle battle, IReadOnlyList<string> log) => new(
        Markup.Escape(battle.Hero.Name),
        _texts[$"class.{battle.Hero.Class.Id}.name"],
        battle.Hero.Class.Color,
        _texts[$"weapon.{battle.Hero.Weapon.Id}"],
        battle.HeroHp,
        battle.Hero.MaxHp,
        _texts[$"enemy.{battle.Enemy.Id}"],
        battle.EnemyHp,
        battle.Enemy.MaxHp,
        battle.Round,
        battle.CanFlee,
        log);

    private string Text(Scene scene, Hero hero) => Fill(_texts[scene.TextKey!], hero);

    private static string Fill(string text, Hero hero) => Localizer.Fill(text, ("name", Markup.Escape(hero.Name)));

    private static int Checked(int index, int count) =>
        index >= 0 && index < count
            ? index
            : throw new ArgumentOutOfRangeException(nameof(index), index, $"The screen answered option {index} of {count}.");
}
