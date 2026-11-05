using ModernUO.Serialization;
using System;
using Server.Engines.Craft;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Clothing/Tangle1.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class Tangle1 : HalfApron
{
    public override int LabelNumber => 1114784; // Tangle

    [Constructible]
    public Tangle1()
        : base()
    {
        Hue = 506;
        Attributes.BonusInt = 10;
        Attributes.DefendChance = 5;
        Attributes.RegenMana = 2;
    }
}

[SerializationGenerator(0, false)]
public partial class GargishTangle1 : GargoyleHalfApron
{
    public override int LabelNumber => 1114784; // Tangle

    [Constructible]
    public GargishTangle1()
        : base()
    {
        Hue = 506;
        Attributes.BonusInt = 10;
        Attributes.DefendChance = 5;
        Attributes.RegenMana = 2;
    }
}
