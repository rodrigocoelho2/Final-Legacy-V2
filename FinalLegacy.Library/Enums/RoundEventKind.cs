namespace FinalLegacy.Library.Enums;

/// <summary>Everything that can happen in a round of combat; each kind has a battle.* text.</summary>
public enum RoundEventKind
{
    HeroHit,
    HeroCrit,
    HeroCounter,
    EnemyDodged,
    EnemyDefended,
    EnemyHit,
    HeroBlocked,
    HeroDodged,
    BothDefend,
    FleeLocked,
    FleeFailed,
    Fled,
}
