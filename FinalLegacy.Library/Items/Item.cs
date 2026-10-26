using FinalLegacy.Library.Combat;
using FinalLegacy.Library.Enums;
using FinalLegacy.Library.Models;

namespace FinalLegacy.Library.Items;

/// <summary>What a weapon does: its damage dice and type, the stat it uses, and whether it takes both hands.</summary>
public sealed record WeaponInfo(Dice Damage, string DamageType, AttackStat Stat, bool TwoHanded, int CritFrom, int HitBonus);

/// <summary>
/// What a piece of armour adds to the armour class (CA). For body armour, <paramref name="Weight"/> also caps how much
/// agility counts, as in D&amp;D.
/// </summary>
public sealed record ArmorInfo(int ArmorClass, ArmorWeight Weight);

/// <summary>A potion: how much it heals.</summary>
public sealed record ConsumableInfo(Dice Heal);

/// <summary>
/// Anything the hero can carry. Its look is a layer in Content/sprites.json (<paramref name="Sprite"/>), drawn over
/// the hero when worn; its name and description live in the texts (item.&lt;id&gt; and item.&lt;id&gt;.desc).
/// </summary>
public sealed record Item(
    string Id,
    ItemSlot Slot,
    Rarity Rarity,
    int Price,
    Stats Modifiers,
    WeaponInfo? Weapon,
    ArmorInfo? Armor,
    ConsumableInfo? Consumable,
    string Sprite)
{
    public bool Equippable => Slot != ItemSlot.None;

    public bool TwoHanded => Weapon?.TwoHanded == true;

    /// <summary>What a merchant pays for it.</summary>
    public int SellPrice => Math.Max(1, Price / 2);
}

/// <summary>A pile of the same item in the bag (potions stack).</summary>
public sealed class ItemStack(Item item, int count)
{
    public Item Item { get; } = item;
    public int Count { get; set; } = count;
}
