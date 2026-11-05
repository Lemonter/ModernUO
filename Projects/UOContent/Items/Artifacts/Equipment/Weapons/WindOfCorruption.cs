using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Weapons/WindOfCorruption.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class WindOfCorruptionHuman : Bow
{
    public override int LabelNumber => 1150358; // Wind of Corruption

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    [Constructible]
    public WindOfCorruptionHuman()
    {
        WeaponAttributes.HitLeechStam = 40;
        Attributes.WeaponSpeed = 30;
        Attributes.WeaponDamage = 50;
        WeaponAttributes.HitLowerDefend = 40;
        AosElementDamages.Chaos = 100;
        Slayer = SlayerName.Fey;
        Hue = 1171;
    }
}
