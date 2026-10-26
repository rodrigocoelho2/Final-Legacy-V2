using FinalLegacy.Library.Adventures;
using FinalLegacy.Library.Combat;
using FinalLegacy.Library.Content;
using FinalLegacy.Library.Models;

namespace FinalLegacy.UnitTest;

/// <summary>Shared content and builders for the tests.</summary>
public static class TestSupport
{
    private static readonly Lazy<GameContent> LazyContent = new(GameContent.Load);

    public static GameContent Content => LazyContent.Value;

    public static GameRules Rules => Content.Rules;

    /// <summary>A hero of a class wearing the kit named after <paramref name="show"/>.</summary>
    public static Hero Hero(string classId, string show, string name = "Rodas")
    {
        var heroClass = Rules.Class(classId);
        return Rules.NewHero(name, heroClass, heroClass.Kits.Single(k => k.Show == show));
    }

    public static Battle Battle(Hero hero, string enemyId, IRandomSource dice, bool canFlee = false) =>
        new(hero, Rules.Enemy(enemyId), canFlee, Rules.Combat, dice);

    /// <summary>The roll that makes a die of <paramref name="sides"/> land on <paramref name="value"/>.</summary>
    public static double Face(int value, int sides = 20) => (value - 0.5) / sides;
}

/// <summary>Loaded dice: hands out the given rolls in order and fails the test if asked for one more.</summary>
public sealed class LoadedDice(params double[] rolls) : IRandomSource
{
    private readonly Queue<double> _rolls = new(rolls);

    public int Left => _rolls.Count;

    public double NextDouble() =>
        _rolls.Count > 0 ? _rolls.Dequeue() : throw new InvalidOperationException("The test ran out of dice rolls.");
}

/// <summary>
/// Dice that always land on the same number. 0.99 means: the enemy always guards, every d20 is a 20 (so the hero
/// always lands a critical, even with disadvantage), every drop chance fails and every random loot roll picks the
/// rarest entry.
/// </summary>
public sealed class FixedDice(double roll) : IRandomSource
{
    public double NextDouble() => roll;
}

/// <summary>
/// A player that answers from a script and remembers everything the game showed, so a whole playthrough can be
/// checked without a console. With no answer left, menus get their first option and the inventory and merchant
/// their last one (close / move on).
/// </summary>
public sealed class ScriptedUi : IGameUi
{
    private readonly Queue<int> _choices = new();
    private readonly Queue<string> _names = new();
    private readonly Queue<int> _gear = new();

    public int ClassChoice { get; init; }
    public int KitChoice { get; init; }
    public int Action { get; init; }

    public List<string> Narrations { get; } = [];
    public List<string> ChoiceTexts { get; } = [];
    public List<string?> Notes { get; } = [];
    public List<string?> NameErrors { get; } = [];
    public List<BattleScreen> BattleScreens { get; } = [];
    public List<string> BattleResults { get; } = [];
    public List<BattleScreen> EndScreens { get; } = [];
    public List<LootScreen> Loot { get; } = [];
    public List<GearScreen> GearScreens { get; } = [];
    public IReadOnlyList<ClassOption> Classes { get; private set; } = [];
    public IReadOnlyList<KitOption> Kits { get; private set; } = [];
    public bool TitleShown { get; private set; }

    public ScriptedUi Choosing(params int[] choices)
    {
        foreach (var choice in choices) _choices.Enqueue(choice);
        return this;
    }

    public ScriptedUi Named(params string[] names)
    {
        foreach (var name in names) _names.Enqueue(name);
        return this;
    }

    /// <summary>Answers for the inventory and merchant screens, in order.</summary>
    public ScriptedUi InGear(params int[] answers)
    {
        foreach (var answer in answers) _gear.Enqueue(answer);
        return this;
    }

    public void ShowTitle() => TitleShown = true;

    public int ChooseClass(IReadOnlyList<ClassOption> options)
    {
        Classes = options;
        return ClassChoice;
    }

    public int ChooseKit(string intro, IReadOnlyList<KitOption> options)
    {
        Kits = options;
        return KitChoice;
    }

    public string AskName(string prompt, string? error)
    {
        NameErrors.Add(error);
        return _names.Count > 0 ? _names.Dequeue() : "Rodas";
    }

    public void Narrate(string text, HeroCard? portrait = null) => Narrations.Add(text);

    public int Choose(string text, IReadOnlyList<string> options, string? note = null)
    {
        ChoiceTexts.Add(text);
        Notes.Add(note);
        return _choices.Count > 0 ? _choices.Dequeue() : throw new InvalidOperationException("The script has no answer left.");
    }

    public int ChooseAction(BattleScreen screen)
    {
        BattleScreens.Add(screen);
        if (BattleScreens.Count > 500) throw new InvalidOperationException("The battle never ends.");
        return Action;
    }

    public void ShowBattleEnd(BattleScreen screen, string result)
    {
        EndScreens.Add(screen);
        BattleResults.Add(result);
    }

    public int ShowLoot(LootScreen screen)
    {
        Loot.Add(screen);
        return 0;
    }

    public int Gear(GearScreen screen)
    {
        GearScreens.Add(screen);
        if (GearScreens.Count > 200) throw new InvalidOperationException("The inventory never closes.");
        return _gear.Count > 0 ? _gear.Dequeue() : screen.Entries.Count - 1;
    }
}
