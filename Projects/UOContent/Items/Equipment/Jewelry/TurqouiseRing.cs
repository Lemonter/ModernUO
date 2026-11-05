using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Jewelry/TurqouiseRing.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class TurqouiseRing : GoldRing
{
    [Constructible]
    public TurqouiseRing()
        : base()
    {
        this.Weight = 1.0;

        BaseRunicTool.ApplyAttributesTo(this, true, 0, Utility.RandomMinMax(1, 3), 0, 100);

        if (Utility.Random(100) < 10)
            this.Attributes.WeaponSpeed += 5;
        else
            this.Attributes.WeaponDamage += 15;
    }

    public override int LabelNumber => 1073460; // turquoise ring
}
