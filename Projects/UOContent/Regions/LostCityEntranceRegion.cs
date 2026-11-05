using System;
using System.Text.Json.Serialization;

namespace Server.Regions;

/// <summary>
///     Портировано из ServUO (Scripts/Regions/DamagingRegion.cs, класс TheLostCityEntry) — вход в
///     Затерянный город: раз в полсекунды сильный яд. Кодара в этом репозитории нет, поэтому исключение
///     для него не переносится — здесь урон получают все.
/// </summary>
public class LostCityEntranceRegion : DamagingRegion
{
    protected override TimeSpan DamageInterval => TimeSpan.FromMilliseconds(500);

    [JsonConstructor] // Don't include parent, since it is special
    public LostCityEntranceRegion(string name, Map map, int priority, params Rectangle3D[] area) : base(name, map, priority, area)
    {
    }

    public LostCityEntranceRegion(string name, Map map, Region parent, params Rectangle3D[] area) : base(name, map, parent, area)
    {
    }

    public LostCityEntranceRegion(string name, Map map, Region parent, int priority, params Rectangle3D[] area)
        : base(name, map, parent, priority, area)
    {
    }

    protected override void OnDamage(Mobile m)
    {
        base.OnDamage(m);

        m.FixedParticles(0x36B0, 1, 14, 0x26BB, 0x3F, 0x7, EffectLayer.Waist);
        m.PlaySound(0x229);

        AOS.Damage(m, Utility.RandomMinMax(10, 20), 0, 0, 0, 100, 0);
    }
}
