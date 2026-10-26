using FinalLegacy.Library.Combat;
using FinalLegacy.Library.Enums;
using FinalLegacy.Library.Items;

namespace FinalLegacy.Library.Models;

/// <summary>
/// One of the original weapon choices, now a starting kit: <paramref name="Show"/> is the item the menu is named after
/// (Escudo de Dorans, Espada de Artúrias…) and <paramref name="Items"/> everything the hero starts wearing.
/// </summary>
public sealed record Kit(string Show, IReadOnlyList<string> Items);

/// <summary>
/// A playable class: base stats, hit points, hair colour (the only thing that tells the bodies apart before the
/// armour goes on), the heaviest armour it is trained in, whether it can use a shield, and its two starting kits.
/// </summary>
public sealed record HeroClass(
    string Id,
    string Color,
    string Hair,
    int MaxHp,
    Stats BaseStats,
    ArmorWeight ArmorTraining,
    bool ShieldTraining,
    IReadOnlyList<Kit> Kits);

/// <summary>A chance of an item dropping, rolled on its own.</summary>
public sealed record LootDrop(string ItemId, double Chance);

/// <summary>How likely a random drop is to be of a rarity; a null rarity means nothing drops.</summary>
public sealed record RarityWeight(Rarity? Rarity, int Weight);

/// <summary>
/// What is found or left behind: gold between two values, items that each drop on their own chance, and
/// <paramref name="Rolls"/> random items, each of a rarity drawn by <paramref name="Weights"/>.
/// </summary>
public sealed record LootTable(
    int GoldMin,
    int GoldMax,
    IReadOnlyList<LootDrop> Drops,
    int Rolls = 0,
    IReadOnlyList<RarityWeight>? Weights = null);

/// <summary>
/// An enemy, D&amp;D style: hit points, armour class, attack bonus and damage. <paramref name="Aggression"/> is the
/// chance it attacks in a round; otherwise it guards, and the hero attacks it with disadvantage.
/// </summary>
public sealed record EnemyType(
    string Id,
    int MaxHp,
    int ArmorClass,
    int AttackBonus,
    Dice Damage,
    string DamageType,
    int Agility,
    double Aggression,
    LootTable Loot,
    string Sprite);

/// <summary>The numbers behind every roll (Content/rules.json → combat).</summary>
public sealed record CombatSettings(
    int BaseArmorClass,
    int Proficiency,
    int MediumArmorAgilityCap,
    int FleeDifficulty);

/// <summary>Everything in Content/rules.json and Content/items.json.</summary>
public sealed class GameRules
{
    private readonly Dictionary<string, HeroClass> _classes;
    private readonly Dictionary<string, Item> _items;
    private readonly Dictionary<string, EnemyType> _enemies;

    public GameRules(
        int nameMaxLength,
        int startGold,
        IReadOnlyList<string> startItems,
        CombatSettings combat,
        IReadOnlyList<HeroClass> classes,
        IReadOnlyList<Item> items,
        IReadOnlyList<EnemyType> enemies)
    {
        NameMaxLength = nameMaxLength;
        StartGold = startGold;
        StartItems = startItems;
        Combat = combat;
        Classes = classes;
        Items = items;
        Enemies = enemies;
        _classes = classes.ToDictionary(c => c.Id);
        _items = items.ToDictionary(i => i.Id);
        _enemies = enemies.ToDictionary(e => e.Id);
    }

    public int NameMaxLength { get; }
    public int StartGold { get; }
    public IReadOnlyList<string> StartItems { get; }
    public CombatSettings Combat { get; }
    public IReadOnlyList<HeroClass> Classes { get; }
    public IReadOnlyList<Item> Items { get; }
    public IReadOnlyList<EnemyType> Enemies { get; }

    public HeroClass Class(string id) => _classes.TryGetValue(id, out var c) ? c : throw Unknown("class", id);
    public Item Item(string id) => _items.TryGetValue(id, out var i) ? i : throw Unknown("item", id);
    public EnemyType Enemy(string id) => _enemies.TryGetValue(id, out var e) ? e : throw Unknown("enemy", id);

    public bool HasItem(string id) => _items.ContainsKey(id);

    /// <summary>The items a random drop of a rarity can be.</summary>
    public IReadOnlyList<Item> OfRarity(Rarity rarity) => Items.Where(i => i.Rarity == rarity).ToList();
    public bool HasEnemy(string id) => _enemies.ContainsKey(id);

    /// <summary>A new hero of a class with one of its kits on and the starting gold and items in the bag.</summary>
    public Hero NewHero(string name, HeroClass heroClass, Kit kit)
    {
        var hero = new Hero(name, heroClass, Combat) { Gold = StartGold };
        foreach (var id in kit.Items)
        {
            hero.Add(Item(id));
            hero.Equip(Item(id));
        }

        foreach (var id in StartItems) hero.Add(Item(id));
        return hero;
    }

    private static KeyNotFoundException Unknown(string what, string id) => new($"Unknown {what} '{id}' in the content.");
}
