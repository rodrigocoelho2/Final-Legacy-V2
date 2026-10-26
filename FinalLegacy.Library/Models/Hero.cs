using FinalLegacy.Library.Combat;
using FinalLegacy.Library.Enums;
using FinalLegacy.Library.Items;

namespace FinalLegacy.Library.Models;

/// <summary>
/// The hero: class, what they wear, what they carry, gold and hit points (which now carry over from battle to
/// battle). Every number the combat uses is worked out here from the class and the equipment, D&amp;D style:
/// <list type="bullet">
///   <item>A stat's modifier is half of it, rounded down (ATK 6 → +3).</item>
///   <item>Armour class (CA) = 10 + the body armour + the agility modifier (all of it in light armour, up to +2 in
///   medium, none in heavy) + the defense modifier + shield, helmet and greaves.</item>
///   <item>Attack bonus = proficiency (+2) + the modifier of the weapon's stat; damage = its dice + that modifier.</item>
///   <item>Armour or a shield the class isn't trained in gives disadvantage on every attack.</item>
/// </list>
/// </summary>
public sealed class Hero
{
    private static readonly WeaponInfo Fists = new(new Dice(1, 2), "bludgeoning", AttackStat.Might, false, 20, 0);

    private readonly Dictionary<ItemSlot, Item> _equipment = [];
    private readonly List<ItemStack> _bag = [];
    private readonly CombatSettings _settings;

    public Hero(string name, HeroClass heroClass, CombatSettings settings)
    {
        Name = name;
        Class = heroClass;
        _settings = settings;
        Hp = heroClass.MaxHp;
    }

    public string Name { get; }
    public HeroClass Class { get; }
    public int Hp { get; set; }
    public int Gold { get; set; }
    public int MaxHp => Class.MaxHp;

    /// <summary>What is worn, slot by slot.</summary>
    public IReadOnlyDictionary<ItemSlot, Item> Equipment => _equipment;

    /// <summary>Everything carried but not worn (worn items are not in the bag).</summary>
    public IReadOnlyList<ItemStack> Bag => _bag;

    public Item? Worn(ItemSlot slot) => _equipment.GetValueOrDefault(slot);

    public Stats Stats =>
        _equipment.Values.Aggregate(Class.BaseStats, (stats, item) => stats + item.Modifiers).AtLeastZero();

    public static int Mod(int stat) => Math.Max(0, stat) / 2;

    public WeaponInfo Weapon => Worn(ItemSlot.MainHand)?.Weapon ?? Fists;

    public int ArmorClass
    {
        get
        {
            var stats = Stats;
            var body = Worn(ItemSlot.Chest)?.Armor;
            var agility = Mod(stats.Agility);
            agility = (body?.Weight ?? ArmorWeight.None) switch
            {
                ArmorWeight.Heavy => 0,
                ArmorWeight.Medium => Math.Min(agility, _settings.MediumArmorAgilityCap),
                _ => agility,
            };

            var pieces = _equipment.Where(e => e.Key != ItemSlot.Chest).Sum(e => e.Value.Armor?.ArmorClass ?? 0);
            return _settings.BaseArmorClass + (body?.ArmorClass ?? 0) + agility + Mod(stats.Defense) + pieces;
        }
    }

    /// <summary>The stat the weapon in hand uses.</summary>
    public int WeaponStat
    {
        get
        {
            var stats = Stats;
            return Weapon.Stat switch
            {
                AttackStat.Finesse => Math.Max(stats.Attack, stats.Agility),
                AttackStat.Arcane => stats.Intelligence,
                _ => stats.Attack,
            };
        }
    }

    public int AttackBonus => _settings.Proficiency + Mod(WeaponStat) + Weapon.HitBonus;

    public Dice Damage => Weapon.Damage.WithBonus(Weapon.Damage.Bonus + Mod(WeaponStat));

    /// <summary>False when the hero wears armour or a shield their class isn't trained in.</summary>
    public bool Proficient =>
        (Worn(ItemSlot.Chest)?.Armor?.Weight ?? ArmorWeight.None) <= Class.ArmorTraining
        && (Class.ShieldTraining || Worn(ItemSlot.OffHand)?.Armor is null);

    public bool IsProficientWith(Item item) => item.Slot switch
    {
        ItemSlot.Chest => (item.Armor?.Weight ?? ArmorWeight.None) <= Class.ArmorTraining,
        ItemSlot.OffHand => Class.ShieldTraining || item.Armor is null,
        _ => true,
    };

    public int Count(string itemId) => _bag.Where(s => s.Item.Id == itemId).Sum(s => s.Count);

    /// <summary>Puts items in the bag; potions pile up, everything else takes its own place.</summary>
    public void Add(Item item, int count = 1)
    {
        if (item.Consumable is not null && _bag.FirstOrDefault(s => s.Item.Id == item.Id) is { } stack)
        {
            stack.Count += count;
            return;
        }

        if (item.Consumable is not null)
        {
            _bag.Add(new ItemStack(item, count));
            return;
        }

        for (var i = 0; i < count; i++) _bag.Add(new ItemStack(item, 1));
    }

    /// <summary>Takes one of an item out of the bag; false if there is none.</summary>
    public bool Remove(Item item)
    {
        var stack = _bag.FirstOrDefault(s => s.Item.Id == item.Id);
        if (stack is null) return false;
        if (--stack.Count == 0) _bag.Remove(stack);
        return true;
    }

    /// <summary>
    /// Wears an item from the bag. Whatever was in its slot goes back to the bag, and so does whatever a two-handed
    /// weapon pushes out (the shield) or pushes a two-handed weapon out.
    /// </summary>
    public void Equip(Item item)
    {
        if (!item.Equippable) throw new InvalidOperationException($"'{item.Id}' can't be worn.");
        if (!Remove(item)) throw new InvalidOperationException($"'{item.Id}' isn't in the bag.");

        Unequip(item.Slot);
        if (item.TwoHanded) Unequip(ItemSlot.OffHand);
        if (item.Slot == ItemSlot.OffHand && Worn(ItemSlot.MainHand)?.TwoHanded == true) Unequip(ItemSlot.MainHand);
        _equipment[item.Slot] = item;
    }

    public void Unequip(ItemSlot slot)
    {
        if (_equipment.Remove(slot, out var item)) Add(item);
    }

    /// <summary>Drinks a potion from the bag and returns the roll (HP never goes over the maximum).</summary>
    public DamageRoll Drink(Item potion, IRandomSource random)
    {
        if (potion.Consumable is null) throw new InvalidOperationException($"'{potion.Id}' can't be drunk.");
        if (!Remove(potion)) throw new InvalidOperationException($"There is no '{potion.Id}' left.");
        var roll = potion.Consumable.Heal.Roll(random);
        Hp = Math.Min(MaxHp, Hp + roll.Total);
        return roll;
    }

    /// <summary>A copy to try things on (the inventory's preview of "what if I wore this?").</summary>
    public Hero Clone()
    {
        var copy = new Hero(Name, Class, _settings) { Hp = Hp, Gold = Gold };
        foreach (var (slot, item) in _equipment) copy._equipment[slot] = item;
        foreach (var stack in _bag) copy._bag.Add(new ItemStack(stack.Item, stack.Count));
        return copy;
    }
}
