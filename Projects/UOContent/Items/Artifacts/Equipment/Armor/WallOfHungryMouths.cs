using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/WallOfHungryMouths.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class WallOfHungryMouths : HeaterShield
{
    [Constructible]
    public WallOfHungryMouths()
    {
        Hue = 1034;
    }

    public override int LabelNumber => 1113722; // Wall of Hungry Mouths

    public override int BasePhysicalResistance => 5;

    public override int BaseFireResistance => 1;

    public override int BaseColdResistance => 0;

    public override int BasePoisonResistance => 0;

    public override int BaseEnergyResistance => 0;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;
}
