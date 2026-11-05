using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Weapons/DeathsHead.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class DeathsHead : DiscMace
{
    public override int LabelNumber => 1113526; // Death's Head

    [Constructible]
    public DeathsHead()
    {
        Hue = 1154;
        WeaponAttributes.HitLightning = 45;
        WeaponAttributes.HitLowerDefend = 30;
        Attributes.WeaponSpeed = 20;
        Attributes.WeaponDamage = 45;
    }

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;
}
