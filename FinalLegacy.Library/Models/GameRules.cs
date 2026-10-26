namespace FinalLegacy.Library.Models;

/// <summary>A playable class: its base stats, the colour its name is drawn in and the weapons it can pick.</summary>
public sealed record HeroClass(string Id, string Color, Stats BaseStats, IReadOnlyList<string> WeaponIds);

/// <summary>A weapon only changes stats; its name lives in the texts (weapon.&lt;id&gt;).</summary>
public sealed record Weapon(string Id, Stats Modifiers);

/// <summary>
/// An enemy. <paramref name="Aggression"/> is the chance (0–1) that it attacks in a round; otherwise it defends.
/// </summary>
public sealed record EnemyType(string Id, int MaxHp, Stats Stats, double Aggression);

/// <summary>The numbers behind every roll in a battle (Content/rules.json → combat).</summary>
public sealed record CombatSettings(
    int AttackMultiplier,
    double CritChancePerAgility,
    double DodgeChancePerAgility,
    double FleeBaseChance,
    double FleeChancePerAgility,
    int MagicDivisor,
    int MinimumHit);

/// <summary>The hero: a name, a class and a weapon. HP is per battle, as in the original.</summary>
public sealed record Hero(string Name, HeroClass Class, Weapon Weapon, int MaxHp)
{
    public Stats Stats => (Class.BaseStats + Weapon.Modifiers).AtLeastZero();
}

/// <summary>Everything in Content/rules.json.</summary>
public sealed class GameRules
{
    private readonly Dictionary<string, HeroClass> _classes;
    private readonly Dictionary<string, Weapon> _weapons;
    private readonly Dictionary<string, EnemyType> _enemies;

    public GameRules(
        int heroMaxHp,
        int nameMaxLength,
        CombatSettings combat,
        IReadOnlyList<HeroClass> classes,
        IReadOnlyList<Weapon> weapons,
        IReadOnlyList<EnemyType> enemies)
    {
        HeroMaxHp = heroMaxHp;
        NameMaxLength = nameMaxLength;
        Combat = combat;
        Classes = classes;
        Weapons = weapons;
        Enemies = enemies;
        _classes = classes.ToDictionary(c => c.Id);
        _weapons = weapons.ToDictionary(w => w.Id);
        _enemies = enemies.ToDictionary(e => e.Id);
    }

    public int HeroMaxHp { get; }
    public int NameMaxLength { get; }
    public CombatSettings Combat { get; }
    public IReadOnlyList<HeroClass> Classes { get; }
    public IReadOnlyList<Weapon> Weapons { get; }
    public IReadOnlyList<EnemyType> Enemies { get; }

    public HeroClass Class(string id) => _classes.TryGetValue(id, out var c) ? c : throw Unknown("class", id);
    public Weapon Weapon(string id) => _weapons.TryGetValue(id, out var w) ? w : throw Unknown("weapon", id);
    public EnemyType Enemy(string id) => _enemies.TryGetValue(id, out var e) ? e : throw Unknown("enemy", id);

    public bool HasWeapon(string id) => _weapons.ContainsKey(id);
    public bool HasEnemy(string id) => _enemies.ContainsKey(id);

    public IReadOnlyList<Weapon> WeaponsOf(HeroClass heroClass) => heroClass.WeaponIds.Select(Weapon).ToList();

    private static KeyNotFoundException Unknown(string what, string id) => new($"Unknown {what} '{id}' in rules.json.");
}
