using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Armor/CrimsonDaggerBelt.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class CrimsonDaggerBelt : DaggerBelt
{
    public override int LabelNumber => 1159213; // crimson dagger belt

    [Constructible]
    public CrimsonDaggerBelt()
        : base()
    {
        Attributes.BonusDex = 5;
        Attributes.BonusHits = 10;
        Attributes.RegenHits = 2;
    }
}
