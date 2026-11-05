using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Armor/CrimsonSwordBelt.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class CrimsonSwordBelt : SwordBelt
{
    public override int LabelNumber => 1159212; // crimson sword belt

    [Constructible]
    public CrimsonSwordBelt()
        : base()
    {
        Attributes.BonusDex = 5;
        Attributes.BonusHits = 10;
        Attributes.RegenHits = 2;
    }
}
