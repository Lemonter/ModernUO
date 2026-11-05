using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Jewelry/FireRubyBracelet.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class FireRubyBracelet : GoldBracelet
{
    [Constructible]
    public FireRubyBracelet()
        : base()
    {
        this.Weight = 1.0;

        BaseRunicTool.ApplyAttributesTo(this, true, 0, Utility.RandomMinMax(1, 4), 0, 100);

        if (Utility.Random(100) < 10)
            this.Attributes.RegenHits += 2;
        else
            this.Resistances.Fire += 10;
    }

    public override int LabelNumber => 1073454; // fire ruby bracelet
}
