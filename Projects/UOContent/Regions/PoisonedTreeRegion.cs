using System;
using System.Text.Json.Serialization;

namespace Server.Regions;

/// <summary>
///     Портировано из ServUO (Scripts/Regions/DamagingRegion.cs, класс PoisonedTree) — отравленное дерево:
///     раз в секунду режет силу и интеллект на 10% (не больше 10 очков) и сообщает о нехватке воздуха.
/// </summary>
public class PoisonedTreeRegion : DamagingRegion
{
    [JsonConstructor] // Don't include parent, since it is special
    public PoisonedTreeRegion(string name, Map map, int priority, params Rectangle3D[] area) : base(name, map, priority, area)
    {
    }

    public PoisonedTreeRegion(string name, Map map, Region parent, params Rectangle3D[] area) : base(name, map, parent, area)
    {
    }

    public PoisonedTreeRegion(string name, Map map, Region parent, int priority, params Rectangle3D[] area)
        : base(name, map, parent, priority, area)
    {
    }

    protected override void OnDamage(Mobile m)
    {
        base.OnDamage(m);

        m.FixedEffect(0x374A, 1, 17);
        m.PlaySound(0x1E1);
        m.LocalOverheadMessage(MessageType.Regular, 0x21, 1074165); // Кружится голова от нехватки воздуха.

        var strMod = Math.Min(10, (int)(m.Str * 0.1));
        var intMod = Math.Min(10, (int)(m.Int * 0.1));

        if (strMod > 0)
        {
            m.AddStatMod(new StatMod(StatType.Str, "Poisoned Tree Str", -strMod, TimeSpan.FromSeconds(1)));
        }

        if (intMod > 0)
        {
            m.AddStatMod(new StatMod(StatType.Int, "Poisoned Tree Int", -intMod, TimeSpan.FromSeconds(1)));
        }
    }
}
