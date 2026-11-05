using System.Text.Json.Serialization;

namespace Server.Regions;

/// <summary>
///     Портировано из ServUO (Scripts/Regions/DamagingRegion.cs, класс CrystalField) — поле кристаллов в
///     Призме Света: при входе звук и сообщение, затем холодовой урон каждую секунду.
/// </summary>
public class CrystalFieldRegion : DamagingRegion
{
    protected override int EnterMessage => 1072396;
    protected override int EnterSound => 0x22F;

    [JsonConstructor] // Don't include parent, since it is special
    public CrystalFieldRegion(string name, Map map, int priority, params Rectangle3D[] area) : base(name, map, priority, area)
    {
    }

    public CrystalFieldRegion(string name, Map map, Region parent, params Rectangle3D[] area) : base(name, map, parent, area)
    {
    }

    public CrystalFieldRegion(string name, Map map, Region parent, int priority, params Rectangle3D[] area)
        : base(name, map, parent, priority, area)
    {
    }

    protected override void OnDamage(Mobile m)
    {
        base.OnDamage(m);

        AOS.Damage(m, Utility.Random(2, 6), 0, 0, 100, 0, 0);
    }
}
