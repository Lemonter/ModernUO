using System;
using Server.Collections;

namespace Server.Spells.Mysticism;

/// <summary>Mysticism 686. Ported from ServUO
/// (Scripts/Spells/Mysticism/SpellDefinitions/MassSleepSpell.cs); registration was already in
/// Spells/Initializer.cs, commented out. All the actual sleeping is delegated to
/// SleepSpell.DoSleep, same as the original.</summary>
public class MassSleepSpell : MysticSpell, ITargetingSpell<IPoint3D>
{
    private static readonly SpellInfo _info = new(
        "Mass Sleep",
        "Vas Zu",
        230,
        9022,
        Reagent.Ginseng,
        Reagent.Nightshade,
        Reagent.SpidersSilk
    );

    public MassSleepSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
    {
    }

    public override SpellCircle Circle => SpellCircle.Fifth;

    public int TargetRange => 12;

    public void Target(IPoint3D p)
    {
        if (!Caster.CanSee(p) || !SpellHelper.CheckTown(p, Caster) || !CheckSequence())
        {
            FinishSequence();
            return;
        }

        SpellHelper.Turn(Caster, p);
        SpellHelper.GetSurfaceTop(ref p);

        var loc = new Point3D(p);
        var map = Caster.Map;

        if (map == null)
        {
            FinishSequence();
            return;
        }

        using var queue = PooledRefQueue<Mobile>.Create();
        foreach (var m in map.GetMobilesInRange(loc, 3))
        {
            if (m == Caster || !SpellHelper.ValidIndirectTarget(Caster, m) ||
                !Caster.CanBeHarmful(m, false))
            {
                continue;
            }

            queue.Enqueue(m);
        }

        while (queue.Count > 0)
        {
            var m = queue.Dequeue();

            var duration = (Caster.Skills[CastSkill].Value + Caster.Skills[DamageSkill].Value) / 20 + 3;
            duration -= GetResistSkill(m) / 10;

            if (duration <= 0)
            {
                continue;
            }

            Caster.DoHarmful(m);
            SleepSpell.DoSleep(Caster, m, TimeSpan.FromSeconds(duration));
        }

        FinishSequence();
    }

    public override void OnCast()
    {
        Caster.Target = new SpellTarget<IPoint3D>(this, allowGround: true);
    }
}
