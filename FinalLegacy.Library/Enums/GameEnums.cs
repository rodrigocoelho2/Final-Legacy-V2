namespace FinalLegacy.Library.Enums;

/// <summary>How rare an item is; each one has its own colour, like in most RPGs.</summary>
public enum Rarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary,
}

/// <summary>Where an item is worn. Consumables have no slot.</summary>
public enum ItemSlot
{
    None,
    Head,
    Chest,
    Legs,
    MainHand,
    OffHand,
    Ring,
}

/// <summary>D&amp;D's armour categories: light lets all of your agility count, medium up to +2, heavy none.</summary>
public enum ArmorWeight
{
    None,
    Light,
    Medium,
    Heavy,
}

/// <summary>Which stat a weapon hits and hurts with.</summary>
public enum AttackStat
{
    /// <summary>ATK — swords, hammers, fists.</summary>
    Might,

    /// <summary>The better of ATK and AGI — knives and light blades.</summary>
    Finesse,

    /// <summary>INT — wands and staves.</summary>
    Arcane,
}

/// <summary>A d20 roll: one die, or two keeping the best (advantage) or the worst (disadvantage).</summary>
public enum RollMode
{
    Normal,
    Advantage,
    Disadvantage,
}

/// <summary>What the hero does in a round.</summary>
public enum CombatAction
{
    Attack,
    Defend,
    UseItem,
    Flee,
}

public enum BattleOutcome
{
    Ongoing,
    Won,
    Lost,
    Fled,
}

/// <summary>Everything that can happen in a round; each kind has a battle.* text.</summary>
public enum RoundEventKind
{
    HeroAttack,
    EnemyAttack,
    HeroGuards,
    EnemyGuards,
    BothGuard,
    GuardOpening,
    HeroHeals,
    FleeLocked,
    FleeFailed,
    Fled,
}
