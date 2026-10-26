using FinalLegacy.Library.Enums;
using FinalLegacy.Library.Models;

namespace FinalLegacy.Library.Adventures;

/// <summary>A class as the class menu shows it.</summary>
public sealed record ClassOption(string Name, string Color, string Blurb, Stats Stats);

/// <summary>A weapon as the weapon menu shows it: its name, what it changes ("+2 DEF  −1 ATK") and the result.</summary>
public sealed record WeaponOption(string Name, string ModifiersText, Stats ResultingStats);

/// <summary>Everything the battle screen draws. <paramref name="Log"/> is what happened in the last round.</summary>
public sealed record BattleScreen(
    string HeroName,
    string ClassName,
    string ClassColor,
    string WeaponName,
    int HeroHp,
    int HeroMaxHp,
    string EnemyName,
    int EnemyHp,
    int EnemyMaxHp,
    int Round,
    bool CanFlee,
    IReadOnlyList<string> Log);

/// <summary>
/// What the adventure needs from a screen. Texts arrive already translated, as markup (see Text/Markup);
/// the console game draws them and the tests answer them with a script.
/// </summary>
public interface IGameUi
{
    /// <summary>The title screen.</summary>
    void ShowTitle();

    /// <summary>The class menu; returns the index of the chosen class.</summary>
    int ChooseClass(IReadOnlyList<ClassOption> options);

    /// <summary>The weapon menu, under the chosen class's line; returns the index of the chosen weapon.</summary>
    int ChooseWeapon(string intro, IReadOnlyList<WeaponOption> options);

    /// <summary>Asks for the hero's name. <paramref name="error"/> is set when the last answer wasn't valid.</summary>
    string AskName(string prompt, string? error);

    /// <summary>Shows a text and waits for the player to carry on.</summary>
    void Narrate(string text);

    /// <summary>
    /// Shows a text with a menu under it and returns the chosen option's index. <paramref name="note"/> is the
    /// answer to the last choice when it didn't move the story on ("Ahmnnn…").
    /// </summary>
    int Choose(string text, IReadOnlyList<string> options, string? note = null);

    /// <summary>The battle screen; returns what the hero does this round.</summary>
    CombatAction ChooseAction(BattleScreen screen);

    /// <summary>The battle screen once more, with how it ended.</summary>
    void ShowBattleEnd(BattleScreen screen, string result);
}
