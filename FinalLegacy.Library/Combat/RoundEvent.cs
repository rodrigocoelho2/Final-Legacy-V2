using FinalLegacy.Library.Enums;

namespace FinalLegacy.Library.Combat;

/// <summary>
/// Something that happened in a round. <paramref name="Damage"/> is what the hit took off,
/// <paramref name="Blocked"/> what a defence stopped and <paramref name="Magic"/> the part of the damage that came from
/// intelligence.
/// </summary>
public sealed record RoundEvent(RoundEventKind Kind, int Damage = 0, int Blocked = 0, int Magic = 0);
