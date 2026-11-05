using System;
using System.Text.Json.Serialization;

namespace Server.Regions;

/// <summary>
///     Портировано из ServUO (Scripts/Regions/DamagingRegion.cs, класс AcidRiver) — кислотная река в
///     Илшенаре: чем ближе к верховью, тем сильнее ожог, а в самой ядовитой части (x &gt; 6484,
///     y &gt; 500) персонаж погибает сразу.
/// </summary>
public class AcidRiverRegion : DamagingRegion
{
    protected override TimeSpan DamageInterval => TimeSpan.FromSeconds(2);

    [JsonConstructor] // Don't include parent, since it is special
    public AcidRiverRegion(string name, Map map, int priority, params Rectangle3D[] area) : base(name, map, priority, area)
    {
    }

    public AcidRiverRegion(string name, Map map, Region parent, params Rectangle3D[] area) : base(name, map, parent, area)
    {
    }

    public AcidRiverRegion(string name, Map map, Region parent, int priority, params Rectangle3D[] area)
        : base(name, map, parent, priority, area)
    {
    }

    protected override void OnDamage(Mobile m)
    {
        base.OnDamage(m);

        if (m.Location.X > 6484 && m.Location.Y > 500)
        {
            m.Kill();
            return;
        }

        m.FixedParticles(0x36B0, 1, 14, 0x26BB, 0x3F, 0x7, EffectLayer.Waist);
        m.PlaySound(0x229);

        var damage = (int)Math.Pow(m.Location.X - 6200, 0.5) + (int)Math.Pow(m.Location.Y - 330, 0.5);

        if (damage > 20)
        {
            m.SendLocalizedMessage(1074567); // Кислота здесь гораздо сильнее — прикосновение убьёт.
        }
        else if (damage > 10)
        {
            m.SendLocalizedMessage(1074566); // Река глубже, концентрация кислоты заметно выше.
        }
        else
        {
            m.SendLocalizedMessage(1074565); // Кислота обжигает кожу.
        }

        AOS.Damage(m, Math.Max(1, damage), 0, 0, 0, 100, 0);
    }
}
