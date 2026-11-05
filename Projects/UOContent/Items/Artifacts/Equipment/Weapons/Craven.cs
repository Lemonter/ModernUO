using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Weapons/Craven.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class Craven : DualPointedSpear
{
    public override int LabelNumber => 1154474; // Craven

    [Constructible]
    public Craven()
    {
        Slayer2 = BaseRunicTool.GetRandomSlayer();
        WeaponAttributes.HitLowerAttack = 40;
        Attributes.WeaponSpeed = 26;
        Attributes.WeaponDamage = 35;
        Attributes.LowerManaCost = 8;
        Hue = 1365;
    }

    public override void GetDamageTypes(Mobile wielder, out int phys, out int fire, out int cold, out int pois, out int nrgy, out int chaos, out int direct)
    {
        phys = 70; cold = 30;
        nrgy = pois = chaos = direct = fire = 0;
    }

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;
}
