using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Ported from ServUO's Shame Revamped dungeon (ShameCrystal.cs) — the currency the dungeon's
///     creatures drop. ServUO converts a stack into account-bound points on double-click
///     (<c>PointsSystem.ShameCrystals</c>); this codebase has no points framework, so crystals stay as
///     stackable items and the guardian altars spend them straight out of the pack.
/// </summary>
[SerializationGenerator(0, false)]
public partial class ShameCrystal : Item
{
    public override int LabelNumber => 1151624; // Crystal of Shame

    [Constructible]
    public ShameCrystal() : this(1)
    {
    }

    [Constructible]
    public ShameCrystal(int amount) : base(16395)
    {
        Stackable = true;
        Amount = amount;

        Hue = 2611;
    }
}
