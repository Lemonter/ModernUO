using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Clothing/MysticsGarb.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class MysticsGarb : Robe
{
    public override int RequiredRaces => Race.AllowGargoylesOnly;

    public override int LabelNumber => 1113649; // Mystic's Garb

    [Constructible]
    public MysticsGarb()
        : base()
    {
        ItemID = 0x4000;
        Hue = 1420;
        Attributes.BonusMana = 5;
        Attributes.LowerManaCost = 1;
    }

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;
}
