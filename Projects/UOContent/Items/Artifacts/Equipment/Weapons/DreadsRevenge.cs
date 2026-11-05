using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Weapons/DreadsRevenge.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class DreadsRevenge : Kryss
{
    public override int LabelNumber => 1072092; // Dread's Revenge

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    [Constructible]
    public DreadsRevenge()
        : base()
    {
        Hue = 0x3A;
        SkillBonuses.SetValues(0, SkillName.Fencing, 20.0);
        WeaponAttributes.HitPoisonArea = 30;
        Attributes.AttackChance = 15;
        Attributes.WeaponSpeed = 50;
    }

    public override void GetDamageTypes(Mobile wielder, out int phys, out int fire, out int cold, out int pois, out int nrgy, out int chaos, out int direct)
    {
        phys = fire = cold = nrgy = chaos = direct = 0;
        pois = 100;
    }
}
