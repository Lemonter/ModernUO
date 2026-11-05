using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Jewelry/EcruCitrineRing.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class EcruCitrineRing : GoldRing
{
    public override int LabelNumber => 1073457; // ecru citrine ring

    [Constructible]
    public EcruCitrineRing()
        : base()
    {
        Weight = 1.0;

        BaseRunicTool.ApplyAttributesTo(this, true, 0, Utility.RandomMinMax(2, 3), 0, 100);

        if (Utility.RandomBool())
            Attributes.EnhancePotions = 50;
        else
            Attributes.BonusStr += 5;
    }
}
