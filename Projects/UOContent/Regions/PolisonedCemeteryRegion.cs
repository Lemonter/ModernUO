using System;
using System.Text.Json.Serialization;

namespace Server.Regions;

/// <summary>
///     Портировано из ServUO (Scripts/Regions/DamagingRegion.cs, класс PoisonedSemetery — опечатка в
///     оригинале) — отравленное кладбище: раз в пять секунд яд и облако спор.
/// </summary>
public class PoisonedCemeteryRegion : DamagingRegion
{
    protected override TimeSpan DamageInterval => TimeSpan.FromSeconds(5);

    [JsonConstructor] // Don't include parent, since it is special
    public PoisonedCemeteryRegion(string name, Map map, int priority, params Rectangle3D[] area) : base(name, map, priority, area)
    {
    }

    public PoisonedCemeteryRegion(string name, Map map, Region parent, params Rectangle3D[] area) : base(name, map, parent, area)
    {
    }

    public PoisonedCemeteryRegion(string name, Map map, Region parent, int priority, params Rectangle3D[] area)
        : base(name, map, parent, priority, area)
    {
    }

    protected override void OnDamage(Mobile m)
    {
        base.OnDamage(m);

        m.FixedParticles(0x36B0, 1, 14, 0x26BB, 0x3F, 0x7, EffectLayer.Waist);
        m.PlaySound(0x229);

        AOS.Damage(m, Utility.Random(2, 3), 0, 0, 0, 100, 0);
    }
}
