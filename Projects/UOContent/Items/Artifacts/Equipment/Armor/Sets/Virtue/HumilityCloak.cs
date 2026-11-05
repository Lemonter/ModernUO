using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/Sets/Virtue/HumilityCloak.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class HumilityCloak : BaseClothing
{
    [Constructible]
    public HumilityCloak()
        : base(0x2B04, Layer.Cloak)
    {
        this.LootType = LootType.Blessed;
        this.Weight = 6.0;
        this.Hue = 0x226;

    }

    public override int LabelNumber => 1075195; // Cloak of Humility (Virtue Armor Set)

    public override int InitMinHits => 0;

    public override int InitMaxHits => 0;

    public override int AosStrReq => 10;
}
