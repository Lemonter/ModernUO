using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/Sets/Virtue/SacrificeSollerets.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class SacrificeSollerets : BaseClothing
{
    [Constructible]
    public SacrificeSollerets()
        : base(0x2B13, Layer.Shoes)
    {
        this.LootType = LootType.Blessed;
        this.Weight = 1.0;
        this.Hue = 0x226;

    }

    public override int LabelNumber => 1075194; // Sollerets of Sacrifice (Virtue Armor Set)

    public override int InitMinHits => 0;

    public override int InitMaxHits => 0;

    public override int AosStrReq => 10;
}
