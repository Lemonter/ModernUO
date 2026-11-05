using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Jewelry/TorcOfTheGuardians.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class TorcOfTheGuardians : GoldNecklace
{
    public override int RequiredRaces => Race.AllowGargoylesOnly;

    public override int LabelNumber => 1113721; // Torc of the Guardians

    [Constructible]
    public TorcOfTheGuardians()
    {
        Hue = 1837;
        Attributes.BonusInt = 5;
        Attributes.BonusStr = 5;
        Attributes.BonusDex = 5;
        Attributes.RegenStam = 2;
        Attributes.RegenMana = 2;
        Attributes.LowerManaCost = 5;
        Resistances.Physical = 5;
        Resistances.Fire = 5;
        Resistances.Cold = 5;
        Resistances.Poison = 5;
        Resistances.Energy = 5;
    }

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;
}
