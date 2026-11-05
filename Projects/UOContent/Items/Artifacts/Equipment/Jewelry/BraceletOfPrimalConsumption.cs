using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Jewelry/BraceletOfPrimalConsumption.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class BraceletOfPrimalConsumption : GoldBracelet
{
    public override int LabelNumber => 1157350; // bracelet of primal consumption

    [Constructible]
    public BraceletOfPrimalConsumption()
    {
        Attributes.Luck = 200;
        Resistances.Physical = 20;
        Resistances.Fire = 20;
        Resistances.Cold = 20;
        Resistances.Poison = 20;
        Resistances.Energy = 20;
    }

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;
}
