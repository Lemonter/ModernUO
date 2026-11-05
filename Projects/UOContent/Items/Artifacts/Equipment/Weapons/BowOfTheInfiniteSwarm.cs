using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Weapons/BowOfTheInfiniteSwarm.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class BowOfTheInfiniteSwarm : CompositeBow
{
    public override int LabelNumber => 1157347; // bow of the infinite swarm

    [Constructible]
    public BowOfTheInfiniteSwarm()
    {
        WeaponAttributes.HitLeechMana = 50;
        WeaponAttributes.HitLeechStam = 50;
        Attributes.BonusStam = 8;
        Attributes.RegenStam = 3;
        Attributes.WeaponSpeed = 30;
        Attributes.WeaponDamage = 50;
    }

    public override void GetDamageTypes(Mobile wielder, out int phys, out int fire, out int cold, out int pois, out int nrgy, out int chaos, out int direct)
    {
        phys = 100;
        fire = cold = nrgy = chaos = direct = pois = 0;
    }

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;
}

[SerializationGenerator(0, false)]
public partial class GlaiveOfTheInfiniteSwarm : SoulGlaive
{
    public override int LabelNumber => 1157366; // Glaive of the Infinite Swarm

    [Constructible]
    public GlaiveOfTheInfiniteSwarm()
    {
        this.WeaponAttributes.HitLeechMana = 50;
        this.WeaponAttributes.HitLeechStam = 50;
        this.Attributes.BonusStam = 8;
        this.Attributes.RegenStam = 3;
        this.Attributes.WeaponSpeed = 30;
        this.Attributes.WeaponDamage = 50;
    }

    public override void GetDamageTypes(Mobile wielder, out int phys, out int fire, out int cold, out int pois, out int nrgy, out int chaos, out int direct)
    {
        phys = 100;
        fire = cold = nrgy = chaos = direct = pois = 0;
    }

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;
}
