using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Weapons/ScepterOfTheChief.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class ScepterOfTheChief : Scepter
{
    public override int LabelNumber => 1072080; // Scepter of the Chief

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    [Constructible]
    public ScepterOfTheChief()
        : base()
    {
        Hue = 0x481;
        Slayer = SlayerName.Exorcism;
        Attributes.RegenHits = 2;
        Attributes.ReflectPhysical = 15;
        Attributes.WeaponDamage = 45;
        WeaponAttributes.HitDispel = 100;
        WeaponAttributes.HitLeechMana = 100;
    }

    public override void GetDamageTypes(Mobile wielder, out int phys, out int fire, out int cold, out int pois, out int nrgy, out int chaos, out int direct)
    {
        phys = fire = cold = nrgy = chaos = direct = 0;
        pois = 100;
    }
}
