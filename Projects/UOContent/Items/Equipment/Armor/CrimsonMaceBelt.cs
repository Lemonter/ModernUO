using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Armor/CrimsonMaceBelt.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class CrimsonMaceBelt : MaceBelt
{
    public override int LabelNumber => 1159211; // crimson mace belt

    [Constructible]
    public CrimsonMaceBelt()
        : base()
    {
        Attributes.BonusDex = 5;
        Attributes.BonusHits = 10;
        Attributes.RegenHits = 2;
    }
}
