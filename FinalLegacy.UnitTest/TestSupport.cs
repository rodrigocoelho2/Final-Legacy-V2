using FinalLegacy.Library.Adventures;
using FinalLegacy.Library.Combat;
using FinalLegacy.Library.Content;
using FinalLegacy.Library.Enums;
using FinalLegacy.Library.Models;

namespace FinalLegacy.UnitTest;

/// <summary>Shared content and builders for the tests.</summary>
public static class TestSupport
{
    private static readonly Lazy<GameContent> LazyContent = new(GameContent.Load);

    public static GameContent Content => LazyContent.Value;

    public static GameRules Rules => Content.Rules;

    public static Hero Hero(string classId, string weaponId, string name = "Rodas") =>
        new(name, Rules.Class(classId), Rules.Weapon(weaponId), Rules.HeroMaxHp);

    public static Battle Battle(Hero hero, string enemyId, IRandomSource dice, bool canFlee = false) =>
        new(hero, Rules.Enemy(enemyId), canFlee, Rules.Combat, dice);
}

/// <summary>Loaded dice: hands out the given rolls in order and fails the test if asked for one more.</summary>
public sealed class LoadedDice(params double[] rolls) : IRandomSource
{
    private readonly Queue<double> _rolls = new(rolls);

    public int Left => _rolls.Count;

    public double NextDouble() =>
        _rolls.Count > 0 ? _rolls.Dequeue() : throw new InvalidOperationException("The test ran out of dice rolls.");
}

/// <summary>Dice that always land on the same number (0.99: the enemy always defends; 0: everything that can happen does).</summary>
public sealed class FixedDice(double roll) : IRandomSource
{
    public double NextDouble() => roll;
}

/// <summary>
/// A player that answers from a script and remembers everything the game showed, so a whole playthrough can be
/// checked without a console.
/// </summary>
public sealed class ScriptedUi : IGameUi
{
    private readonly Queue<int> _choices = new();
    private readonly Queue<string> _names = new();

    public int ClassChoice { get; init; }
    public int WeaponChoice { get; init; }
    public CombatAction Action { get; init; } = CombatAction.Attack;

    public List<string> Narrations { get; } = [];
    public List<string> ChoiceTexts { get; } = [];
    public List<string?> Notes { get; } = [];
    public List<string?> NameErrors { get; } = [];
    public List<BattleScreen> BattleScreens { get; } = [];
    public List<string> BattleResults { get; } = [];
    public IReadOnlyList<ClassOption> Classes { get; private set; } = [];
    public IReadOnlyList<WeaponOption> Weapons { get; private set; } = [];
    public bool TitleShown { get; private set; }

    /// <summary>The answers to the story's menus, in order (0 = the first option).</summary>
    public ScriptedUi Choosing(params int[] choices)
    {
        foreach (var choice in choices) _choices.Enqueue(choice);
        return this;
    }

    /// <summary>What the player types when asked for a name, in order.</summary>
    public ScriptedUi Named(params string[] names)
    {
        foreach (var name in names) _names.Enqueue(name);
        return this;
    }

    public void ShowTitle() => TitleShown = true;

    public int ChooseClass(IReadOnlyList<ClassOption> options)
    {
        Classes = options;
        return ClassChoice;
    }

    public int ChooseWeapon(string intro, IReadOnlyList<WeaponOption> options)
    {
        Weapons = options;
        return WeaponChoice;
    }

    public string AskName(string prompt, string? error)
    {
        NameErrors.Add(error);
        return _names.Count > 0 ? _names.Dequeue() : "Rodas";
    }

    public void Narrate(string text) => Narrations.Add(text);

    public int Choose(string text, IReadOnlyList<string> options, string? note = null)
    {
        ChoiceTexts.Add(text);
        Notes.Add(note);
        return _choices.Count > 0 ? _choices.Dequeue() : throw new InvalidOperationException("The script has no answer left.");
    }

    public CombatAction ChooseAction(BattleScreen screen)
    {
        BattleScreens.Add(screen);
        if (BattleScreens.Count > 500) throw new InvalidOperationException("The battle never ends.");
        return Action;
    }

    public void ShowBattleEnd(BattleScreen screen, string result) => BattleResults.Add(result);
}
