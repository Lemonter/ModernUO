using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Weapons/Glenda.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class Glenda : Club
{
    public override int LabelNumber => 1157346; // glenda

    [Constructible]
    public Glenda()
    {
        WeaponAttributes.HitLeechMana = 20;
        WeaponAttributes.HitLowerDefend = 70;
        Attributes.BonusStr = 16;
        Attributes.WeaponDamage = 100;
    }

    public override void GetDamageTypes(Mobile wielder, out int phys, out int fire, out int cold, out int pois, out int nrgy, out int chaos, out int direct)
    {
        phys = 100;
        fire = cold = nrgy = chaos = direct = pois = 0;
    }

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;
}
