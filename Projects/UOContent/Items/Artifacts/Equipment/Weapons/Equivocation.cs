using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Weapons/Equivocation.cs).
///     GargishEquivocation: GargishGnarledStaff has no counterpart here; re-based on BaseStaff with its ServUO art/stat line (`0x48B8`).
/// </summary>
[SerializationGenerator(0, false)]
public partial class Equivocation : GnarledStaff
{
    public override int LabelNumber => 1154473; // Equivocation

    [Constructible]
    public Equivocation()
    {
        Slayer2 = BaseRunicTool.GetRandomSlayer();
        Attributes.AttackChance = 10;
        Attributes.RegenHits = 6;
        Attributes.WeaponSpeed = 35;
        Attributes.WeaponDamage = 50;
        Hue = 1365;
    }

    public override void GetDamageTypes(Mobile wielder, out int phys, out int fire, out int cold, out int pois, out int nrgy, out int chaos, out int direct)
    {
        phys = pois = 50;
        cold = nrgy = chaos = direct = fire = 0;
    }

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;
}

[SerializationGenerator(0, false)]
public partial class GargishEquivocation : BaseStaff
{
    public override double DefaultWeight => 3.0;

    public override int LabelNumber => 1154473; // Equivocation

    [Constructible]
    public GargishEquivocation() : base(0x48B8)
    {
        Slayer2 = BaseRunicTool.GetRandomSlayer();
        Attributes.AttackChance = 10;
        Attributes.RegenHits = 6;
        Attributes.WeaponSpeed = 35;
        Attributes.WeaponDamage = 50;
        Hue = 1365;
    }

    public override void GetDamageTypes(Mobile wielder, out int phys, out int fire, out int cold, out int pois, out int nrgy, out int chaos, out int direct)
    {
        phys = pois = 50;
        cold = nrgy = chaos = direct = fire = 0;
    }

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;
}
