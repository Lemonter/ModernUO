using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Weapons/Pincer.cs).
///     GargishPincer: GargishBattleAxe has no counterpart here; re-based on BaseAxe with its ServUO art/stat line (`0x48B0`).
/// </summary>
[SerializationGenerator(0, false)]
public partial class Pincer : BattleAxe
{
    public override int LabelNumber => 1154476; // Pincer

    [Constructible]
    public Pincer()
    {
        Hue = 2500;
        Slayer2 = BaseRunicTool.GetRandomSlayer();
        WeaponAttributes.HitLeechHits = 87;
        Attributes.RegenStam = 6;
        Attributes.RegenMana = 6;
        Attributes.WeaponDamage = 40;
    }

    public override void GetDamageTypes(Mobile wielder, out int phys, out int fire, out int cold, out int pois, out int nrgy, out int chaos, out int direct)
    {
        phys = 30; fire = 70;
        cold = nrgy = chaos = direct = pois = 0;
    }

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;
}

[SerializationGenerator(0, false)]
public partial class GargishPincer : BaseAxe
{
    public override double DefaultWeight => 4.0;

    public override int LabelNumber => 1154476; // Pincer

    [Constructible]
    public GargishPincer() : base(0x48B0)
    {
        Hue = 2500;
        Slayer2 = BaseRunicTool.GetRandomSlayer();
        WeaponAttributes.HitLeechHits = 87;
        Attributes.RegenStam = 6;
        Attributes.RegenMana = 6;
        Attributes.WeaponDamage = 40;
    }

    public override void GetDamageTypes(Mobile wielder, out int phys, out int fire, out int cold, out int pois, out int nrgy, out int chaos, out int direct)
    {
        phys = 30; fire = 70;
        cold = nrgy = chaos = direct = pois = 0;
    }

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;
}
