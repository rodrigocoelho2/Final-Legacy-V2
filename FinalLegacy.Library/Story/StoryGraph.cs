using FinalLegacy.Library.Models;

namespace FinalLegacy.Library.Story;

public enum SceneKind
{
    /// <summary>Text, then on to <see cref="Scene.Goto"/>.</summary>
    Narration,

    /// <summary>Text and a menu; each choice goes somewhere or answers with a reply and asks again.</summary>
    Choice,

    /// <summary>A fight; <see cref="Scene.Win"/> on a win, <see cref="Scene.Flee"/> if the hero gets away.</summary>
    Battle,

    /// <summary>Text, then something found (gold and items), then on to <see cref="Scene.Goto"/>.</summary>
    Loot,

    /// <summary>Text, then a merchant: buy, sell, rest by the fire; then on to <see cref="Scene.Goto"/>.</summary>
    Shop,

    /// <summary>The last text of a chapter.</summary>
    End,
}

/// <summary>What a loot scene gives: gold, set items (repeat an id for more) and random rolls like an enemy's.</summary>
public sealed record LootSpec(int Gold, IReadOnlyList<string> Items, LootTable Random);

/// <summary>One line of a merchant's stock; no <paramref name="Count"/> means it never runs out.</summary>
public sealed record ShopStock(string ItemId, int? Count);

/// <summary>A merchant: what they sell and what a night by their fire costs (0 = no resting).</summary>
public sealed record ShopSpec(IReadOnlyList<ShopStock> Stock, int RestPrice);

/// <summary>
/// One option of a choice scene. It either moves the story on (<paramref name="Goto"/>) or only answers with
/// <paramref name="ReplyKey"/> and asks again, like "Não" in the original's cave.
/// </summary>
public sealed record StoryChoice(string LabelKey, string? Goto, string? ReplyKey);

/// <summary>Which enemy a battle scene is against and whether the hero may run away.</summary>
public sealed record BattleSpec(string EnemyId, bool CanFlee);

/// <summary>A scene of Content/story.json. Text keys point into the language files.</summary>
public sealed record Scene(
    string Id,
    string? TextKey,
    IReadOnlyList<StoryChoice> Choices,
    string? Goto,
    BattleSpec? Battle,
    string? Win,
    string? Flee,
    bool End,
    LootSpec? Loot = null,
    ShopSpec? Shop = null)
{
    public SceneKind Kind =>
        Battle is not null ? SceneKind.Battle
        : Shop is not null ? SceneKind.Shop
        : Loot is not null ? SceneKind.Loot
        : Choices.Count > 0 ? SceneKind.Choice
        : End ? SceneKind.End
        : SceneKind.Narration;

    /// <summary>Every scene this one can lead to.</summary>
    public IEnumerable<string> Exits()
    {
        if (Goto is not null) yield return Goto;
        if (Win is not null) yield return Win;
        if (Flee is not null) yield return Flee;
        foreach (var choice in Choices)
        {
            if (choice.Goto is not null) yield return choice.Goto;
        }
    }
}

/// <summary>The story: scenes by id and the one it starts at.</summary>
public sealed class StoryGraph(string start, IReadOnlyDictionary<string, Scene> scenes)
{
    public string Start { get; } = start;

    public IReadOnlyDictionary<string, Scene> Scenes { get; } = scenes;

    public Scene this[string id] =>
        Scenes.TryGetValue(id, out var scene) ? scene : throw new KeyNotFoundException($"Unknown scene '{id}' in story.json.");
}
