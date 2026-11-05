using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Jewelry/DragonJadeEarrings.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class DragonJadeEarrings : GargishEarrings
{
    public override int RequiredRaces => Race.AllowGargoylesOnly;

    public override int LabelNumber => 1113720; // Dragon Jade Earrings

    [Constructible]
    public DragonJadeEarrings()
    {
        Hue = 2129;
        Attributes.BonusDex = 5;
        Attributes.BonusStr = 5;
        Attributes.RegenHits = 2;
        Attributes.RegenStam = 3;
        Attributes.LowerManaCost = 5;
    }

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;
}
