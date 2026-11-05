using System.Text.Json.Serialization;

namespace Server.Regions;

/// <summary>
///     Портировано из ServUO (Scripts/Regions/DamagingRegion.cs, класс IcyRiver) — ледяная река
///     Илшенара: холодовой урон каждую секунду.
/// </summary>
public class IcyRiverRegion : DamagingRegion
{
    [JsonConstructor] // Don't include parent, since it is special
    public IcyRiverRegion(string name, Map map, int priority, params Rectangle3D[] area) : base(name, map, priority, area)
    {
    }

    public IcyRiverRegion(string name, Map map, Region parent, params Rectangle3D[] area) : base(name, map, parent, area)
    {
    }

    public IcyRiverRegion(string name, Map map, Region parent, int priority, params Rectangle3D[] area)
        : base(name, map, parent, priority, area)
    {
    }

    protected override void OnDamage(Mobile m)
    {
        base.OnDamage(m);

        AOS.Damage(m, Utility.Random(2, 3), 0, 0, 100, 0, 0);
    }
}
