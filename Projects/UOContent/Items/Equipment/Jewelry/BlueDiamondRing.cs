using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Jewelry/BlueDiamondRing.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class BlueDiamondRing : GoldRing
{
    [Constructible]
    public BlueDiamondRing()
        : base()
    {
        this.Weight = 1.0;

        BaseRunicTool.ApplyAttributesTo(this, true, 0, Utility.RandomMinMax(2, 4), 0, 100);

        switch ( Utility.Random(4) )
        {
            case 0:
                this.Attributes.LowerManaCost += 10;
                break;
            case 1:
                this.Attributes.CastSpeed += 1;
                break;
            case 2:
                this.Attributes.CastRecovery += 2;
                break;
            case 3:
                this.Attributes.SpellDamage += 5;
                break;
        }
    }

    public override int LabelNumber => 1073458; // blue diamond ring
}
