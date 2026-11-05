using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Weapons/TheDeceiver.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class TheDeceiver : TwoHandedAxe
{
    public override int LabelNumber => 1157344; // the deceiver

    [Constructible]
    public TheDeceiver()
    {
        WeaponAttributes.HitLowerAttack = 20;
        WeaponAttributes.HitEnergyArea = 75;
        WeaponAttributes.HitLowerDefend = 20;
        WeaponAttributes.HitLeechStam = 30;
        Attributes.LowerManaCost = 8;
        Attributes.WeaponDamage = 75;
    }

    public override void GetDamageTypes(Mobile wielder, out int phys, out int fire, out int cold, out int pois, out int nrgy, out int chaos, out int direct)
    {
        phys = 100;
        fire = cold = nrgy = chaos = direct = pois = 0;
    }

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;
}
