using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Weapons/Abhorrence.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class Abhorrence : Crossbow
{
    public override int LabelNumber => 1154472; // Abhorrence

    [Constructible]
    public Abhorrence()
    {
        Attributes.SpellChanneling = 1;
        WeaponAttributes.HitLightning = 35;
        WeaponAttributes.HitLeechMana = 40;
        WeaponAttributes.HitLowerDefend = 20;
        Attributes.WeaponSpeed = 35;
        Attributes.WeaponDamage = 50;
        Hue = 1910;
    }

    public override void GetDamageTypes(Mobile wielder, out int phys, out int fire, out int cold, out int pois, out int nrgy, out int chaos, out int direct)
    {
        nrgy = 100;
        phys = pois = cold = chaos = direct = fire = 0;
    }

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;
}
