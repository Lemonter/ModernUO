using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Jewelry/DarkSapphireBracelet.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class DarkSapphireBracelet : GoldBracelet
{
    [Constructible]
    public DarkSapphireBracelet()
        : base()
    {
        this.Weight = 1.0;

        BaseRunicTool.ApplyAttributesTo(this, true, 0, Utility.RandomMinMax(1, 4), 0, 100);

        if (Utility.Random(100) < 10)
            this.Attributes.RegenMana += 2;
        else
            this.Resistances.Cold += 10;
    }

    public override int LabelNumber => 1073455; // dark sapphire bracelet
}
