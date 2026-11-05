using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Jewelry/PerfectEmeraldRing.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class PerfectEmeraldRing : GoldRing
{
    [Constructible]
    public PerfectEmeraldRing()
        : base()
    {
        this.Weight = 1.0;

        BaseRunicTool.ApplyAttributesTo(this, true, 0, Utility.RandomMinMax(2, 4), 0, 100);

        if (Utility.RandomBool())
            this.Resistances.Poison += 10;
        else
            this.Attributes.SpellDamage += 5;
    }

    public override int LabelNumber => 1073459; // perfect emerald ring
}
