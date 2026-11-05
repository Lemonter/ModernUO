namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/RepairBench.cs, originally declared with RepairDeed).
///     ServUO's repair-deed skill selector; kept local to the repair bench port because this codebase has no
///     equivalent enum.
/// </summary>
public enum RepairSkillType
{
    Smithing,
    Tailoring,
    Tinkering,
    Carpentry,
    Fletching,
    Masonry,
    Glassblowing
}
