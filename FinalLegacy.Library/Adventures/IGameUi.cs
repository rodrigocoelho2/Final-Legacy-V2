using FinalLegacy.Library.Models;
using FinalLegacy.Library.Sprites;

namespace FinalLegacy.Library.Adventures;

/// <summary>The hero as every screen shows them: the numbers that matter and how they look right now.</summary>
public sealed record HeroCard(
    string Name,
    string ClassName,
    string ClassColor,
    int Hp,
    int MaxHp,
    int ArmorClass,
    int AttackBonus,
    string DamageText,
    Stats Stats,
    int Gold,
    bool Proficient,
    PixelGrid Look);

/// <summary>An item as the inventory, the merchant and the loot screens show it.</summary>
public sealed record ItemCard(
    string Name,
    string RarityName,
    string RarityColor,
    string TypeLine,
    IReadOnlyList<string> Effects,
    string Description,
    PixelGrid Icon,
    int Count);

/// <summary>
/// A line of the inventory or of the merchant's list. <paramref name="Preview"/> is the hero as they would be after
/// choosing it (wearing the item, without it, after buying…), so the screen can show the difference.
/// </summary>
public sealed record GearEntry(
    string Section,
    string Label,
    string? Right,
    string? Hint,
    bool Enabled,
    ItemCard? Item,
    HeroCard? Preview);

/// <summary>The inventory or a merchant: the hero, a list to choose from and the result of the last choice.</summary>
public sealed record GearScreen(string Title, string? Intro, HeroCard Hero, IReadOnlyList<GearEntry> Entries, string? Message);

/// <summary>What was found: the text, gold and items, and what to do next.</summary>
public sealed record LootScreen(string Text, int Gold, IReadOnlyList<ItemCard> Items, IReadOnlyList<string> Options);

/// <summary>A class as the class menu shows it, wearing its first kit.</summary>
public sealed record ClassOption(string Name, string Color, string Blurb, Stats Stats, int MaxHp, string Training, PixelGrid Look);

/// <summary>One of the original weapon choices: its name, what it changes, the whole kit and the hero wearing it.</summary>
public sealed record KitOption(string Name, string ModifiersText, string ItemsText, HeroCard Preview);

/// <summary>A battle option: its label and, when it can't be used now, why.</summary>
public sealed record ActionOption(string Label, string? Hint);

/// <summary>Everything the battle screen draws. <paramref name="Log"/> is what happened in the last round.</summary>
public sealed record BattleScreen(
    HeroCard Hero,
    string EnemyName,
    int EnemyHp,
    int EnemyMaxHp,
    int EnemyArmorClass,
    PixelGrid EnemyLook,
    int Round,
    bool Opening,
    IReadOnlyList<string> Log,
    IReadOnlyList<ActionOption> Actions);

/// <summary>
/// What the adventure needs from a screen. Texts arrive translated, as markup (see Text/Markup), and pictures as
/// pixel grids; the console game draws them and the tests answer them with a script.
/// </summary>
public interface IGameUi
{
    void ShowTitle();

    /// <summary>The class menu; returns the index of the chosen class.</summary>
    int ChooseClass(IReadOnlyList<ClassOption> options);

    /// <summary>The weapon (kit) menu under the class's line; returns the index of the chosen kit.</summary>
    int ChooseKit(string intro, IReadOnlyList<KitOption> options);

    /// <summary>Asks for the hero's name. <paramref name="error"/> is set when the last answer wasn't valid.</summary>
    string AskName(string prompt, string? error);

    /// <summary>Shows a text (with the hero's portrait, when given) and waits for the player.</summary>
    void Narrate(string text, HeroCard? portrait = null);

    /// <summary>A text with a menu under it; returns the chosen index. <paramref name="note"/> answers the last choice.</summary>
    int Choose(string text, IReadOnlyList<string> options, string? note = null);

    /// <summary>The battle screen; returns the index of the chosen action.</summary>
    int ChooseAction(BattleScreen screen);

    /// <summary>The battle screen once more, with how it ended.</summary>
    void ShowBattleEnd(BattleScreen screen, string result);

    /// <summary>What was found; returns the chosen option.</summary>
    int ShowLoot(LootScreen screen);

    /// <summary>The inventory or a merchant; returns the index of the chosen entry.</summary>
    int Gear(GearScreen screen);
}
