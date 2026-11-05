using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Jewelry/WhitePearlBracelet.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class WhitePearlBracelet : GoldBracelet
{
    [Constructible]
    public WhitePearlBracelet()
        : base()
    {
        this.Weight = 1.0;

        this.Attributes.NightSight = 1;

        BaseRunicTool.ApplyAttributesTo(this, true, 0, Utility.RandomMinMax(3, 5), 0, 100);

        if (Utility.Random(100) < 50)
        {
            switch ( Utility.Random(3) )
            {
                case 0:
                    this.Attributes.CastSpeed += 1;
                    break;
                case 1:
                    this.Attributes.CastRecovery += 2;
                    break;
                case 2:
                    this.Attributes.LowerRegCost += 10;
                    break;
            }
        }
    }

    public override int LabelNumber => 1073456; // white pearl bracelet
}
