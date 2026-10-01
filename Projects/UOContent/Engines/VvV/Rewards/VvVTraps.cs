using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Items;
using Server.Mobiles;
using Server.Spells;
using Server.Spells.Necromancy;

namespace Server.Engines.VvV;

// Ported from real OSI/ServUO content (Scripts/Services/ViceVsVirtue/Items/Rewards/VvVTraps.cs).
public enum VvVTrapType
{
    Explosion = 1015027, // Explosion
    Poison = 1028000,    // Poison
    Cold = 1113466,      // Freezing
    Energy = 1154942,    // Shocking
    Blade = 1154943      // Blades
}

public enum DeploymentType
{
    Proximaty = 1154939,
    Tripwire = 1154940
}

[SerializationGenerator(0, false)]
public partial class VvVTrap : Item, IRevealableItem
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Mobile _owner;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private DeploymentType _deploymentType;

    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private VvVTrap _parentTrap;

    [SerializableField(3)]
    private List<VvVTrap> _links;

    public override bool HandlesOnMovement => true;
    public bool CheckWhenHidden => true;

    public virtual int MinDamage => 0;
    public virtual int MaxDamage => 0;
    public virtual VvVTrapType TrapType => VvVTrapType.Explosion;

    public static int HiddenID = 8600;
    public static int VisibleID = 39818;

    [Constructible]
    public VvVTrap() : this(null, DeploymentType.Proximaty)
    {
    }

    public VvVTrap(Mobile owner, DeploymentType type) : base(HiddenID)
    {
        _owner = owner;
        _deploymentType = type;

        Movable = false;
        Hue = 0x3D8;
    }

    // ServUO traces the wire with `MovementPath` (a mobile-pathfinding helper), which only
    // has a Mobile-based constructor in this codebase (no two-Point3D overload) — replaced
    // with a direct point-to-point line (Bresenham), tracing the same straight tripwire
    // without needing a walkable path.
    public bool SetTripwire(VvVTrapKit deed, Point3D myLocation, Point3D wireLocation, Map map)
    {
        Links = new List<VvVTrap>();

        foreach (var p in TraceLine(myLocation, wireLocation, map))
        {
            if (p == myLocation)
            {
                continue;
            }

            var trap = deed.ConstructTrap(Owner);
            Links.Add(trap);
            trap.ParentTrap = this;

            trap.MoveToWorld(p, map);
        }

        return true;
    }

    private static IEnumerable<Point3D> TraceLine(Point3D from, Point3D to, Map map)
    {
        var x0 = from.X;
        var y0 = from.Y;
        var x1 = to.X;
        var y1 = to.Y;

        var dx = Math.Abs(x1 - x0);
        var dy = -Math.Abs(y1 - y0);
        var sx = x0 < x1 ? 1 : -1;
        var sy = y0 < y1 ? 1 : -1;
        var err = dx + dy;

        while (true)
        {
            yield return new Point3D(x0, y0, map.GetAverageZ(x0, y0));

            if (x0 == x1 && y0 == y1)
            {
                yield break;
            }

            var e2 = 2 * err;

            if (e2 >= dy)
            {
                err += dy;
                x0 += sx;
            }

            if (e2 <= dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    }

    public override void OnMovement(Mobile m, Point3D oldLocation)
    {
        if (IsEnemy(m) && DeploymentType == DeploymentType.Proximaty && m.InRange(Location, 3) && ViceVsVirtueSystem.IsEnemy(m, Owner))
        {
            Detonate(m);
        }
    }

    public bool CheckReveal(Mobile m)
    {
        if (!ViceVsVirtueSystem.IsVvV(m) || ItemID != HiddenID)
        {
            return false;
        }

        return Utility.Random(100) <= m.Skills[SkillName.DetectHidden].Value;
    }

    public void OnRevealed(Mobile m)
    {
        ItemID = VisibleID;

        if (Links != null)
        {
            foreach (var l in Links)
            {
                if (!l.Deleted && l.ItemID == HiddenID)
                {
                    l.ItemID = VisibleID;
                }
            }
        }

        if (ParentTrap != null)
        {
            if (ParentTrap.ItemID == HiddenID)
            {
                ParentTrap.ItemID = VisibleID;
            }

            ParentTrap.OnRevealed(m);
        }
    }

    public bool CheckPassiveDetect(Mobile m)
    {
        if (m.InRange(Location, 6))
        {
            var skill = (int)m.Skills[SkillName.DetectHidden].Value;

            if (skill >= 80 && Utility.Random(600) < skill)
            {
                m.PrivateOverheadMessage(MessageType.Regular, 0x21, 500813, m.NetState); // [trapped]
            }
        }

        return false;
    }

    public override bool OnMoveOver(Mobile m)
    {
        if (IsEnemy(m))
        {
            Detonate(m);
        }

        return base.OnMoveOver(m);
    }

    public bool IsEnemy(Mobile m) => Owner == null ||
        (ViceVsVirtueSystem.IsVvV(m) && ViceVsVirtueSystem.IsVvV(Owner) && ViceVsVirtueSystem.IsEnemy(m, Owner));

    public virtual void Detonate(Mobile m)
    {
        Owner?.DoHarmful(m);
        Delete();
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();

        if (Links != null)
        {
            foreach (var l in Links)
            {
                if (!l.Deleted)
                {
                    l.Delete();
                }
            }
        }

        if (ParentTrap is { Deleted: false })
        {
            ParentTrap.Delete();
        }
    }
}

[SerializationGenerator(0, false)]
public partial class VvVExplosionTrap : VvVTrap
{
    public override int MinDamage => 40;
    public override int MaxDamage => 50;

    [Constructible]
    public VvVExplosionTrap() : this(null, DeploymentType.Proximaty)
    {
    }

    public VvVExplosionTrap(Mobile owner, DeploymentType type) : base(owner, type)
    {
    }

    public override void Detonate(Mobile m)
    {
        var dam = Utility.RandomMinMax(MinDamage, MaxDamage);

        if (DeploymentType == DeploymentType.Tripwire)
        {
            dam *= 2;
        }

        AOS.Damage(m, Owner, dam, 50, 50, 0, 0, 0);

        Effects.SendLocationEffect(GetWorldLocation(), Map, 0x36BD, 15, 10);
        Effects.PlaySound(GetWorldLocation(), Map, 0x307);

        base.Detonate(m);
    }
}

[SerializationGenerator(0, false)]
public partial class VvVPoisonTrap : VvVTrap
{
    public override int MinDamage => 25;
    public override int MaxDamage => 35;
    public override VvVTrapType TrapType => VvVTrapType.Poison;

    [Constructible]
    public VvVPoisonTrap() : this(null, DeploymentType.Proximaty)
    {
    }

    public VvVPoisonTrap(Mobile owner, DeploymentType type) : base(owner, type)
    {
    }

    public override void Detonate(Mobile m)
    {
        var dam = Utility.RandomMinMax(MinDamage, MaxDamage);

        if (DeploymentType == DeploymentType.Tripwire)
        {
            dam *= 2;
        }

        AOS.Damage(m, Owner, dam, 0, 0, 0, 100, 0);
        m.ApplyPoison(Owner, Poison.Deadly);

        Effects.SendTargetEffect(m, 0x1145, 3, 16);
        Effects.PlaySound(GetWorldLocation(), Map, 0x230);

        base.Detonate(m);
    }
}

[SerializationGenerator(0, false)]
public partial class VvVColdTrap : VvVTrap
{
    public override int MinDamage => 25;
    public override int MaxDamage => 35;
    public override VvVTrapType TrapType => VvVTrapType.Cold;

    [Constructible]
    public VvVColdTrap() : this(null, DeploymentType.Proximaty)
    {
    }

    public VvVColdTrap(Mobile owner, DeploymentType type) : base(owner, type)
    {
    }

    public override void Detonate(Mobile m)
    {
        var dam = Utility.RandomMinMax(MinDamage, MaxDamage);

        if (DeploymentType == DeploymentType.Tripwire)
        {
            dam *= 2;
        }

        AOS.Damage(m, Owner, dam, 0, 0, 100, 0, 0);
        m.FixedParticles(0x374A, 1, 15, 9502, 97, 3, (EffectLayer)255);

        m.Paralyze(TimeSpan.FromSeconds(5));

        Effects.SendLocationParticles(m, 0x374A, 1, 30, 97, 3, 9502, 0);
        Effects.PlaySound(GetWorldLocation(), Map, 0x1FB);

        base.Detonate(m);
    }
}

[SerializationGenerator(0, false)]
public partial class VvVEnergyTrap : VvVTrap
{
    public override int MinDamage => 25;
    public override int MaxDamage => 35;
    public override VvVTrapType TrapType => VvVTrapType.Energy;

    [Constructible]
    public VvVEnergyTrap() : this(null, DeploymentType.Proximaty)
    {
    }

    public VvVEnergyTrap(Mobile owner, DeploymentType type) : base(owner, type)
    {
    }

    public override void Detonate(Mobile m)
    {
        var dam = Utility.RandomMinMax(MinDamage, MaxDamage);

        if (DeploymentType == DeploymentType.Tripwire)
        {
            dam *= 2;
        }

        Effects.SendBoltEffect(m, true, 0);
        AOS.Damage(m, Owner, dam, 0, 0, 100, 0, 0);

        MortalStrike.BeginWound(m, TimeSpan.FromSeconds(3));

        base.Detonate(m);
    }
}

[SerializationGenerator(0, false)]
public partial class VvVBladeTrap : VvVTrap
{
    public override int MinDamage => 25;
    public override int MaxDamage => 35;
    public override VvVTrapType TrapType => VvVTrapType.Blade;

    [Constructible]
    public VvVBladeTrap() : this(null, DeploymentType.Proximaty)
    {
    }

    public VvVBladeTrap(Mobile owner, DeploymentType type) : base(owner, type)
    {
    }

    public override void Detonate(Mobile m)
    {
        var dam = Utility.RandomMinMax(MinDamage, MaxDamage);

        if (DeploymentType == DeploymentType.Tripwire)
        {
            dam *= 2;
        }

        AOS.Damage(m, Owner, dam, 100, 0, 0, 0, 0);
        Effects.SendLocationEffect(m.Location, m.Map, 0x11AD, 25, 10);
        Effects.PlaySound(m.Location, m.Map, 0x218);

        var context = TransformationSpellHelper.GetContext(m);

        if ((context != null && (context.Type == typeof(LichFormSpell) || context.Type == typeof(WraithFormSpell))) ||
            (m is BaseCreature { BleedImmune: true }))
        {
            return;
        }

        m.SendLocalizedMessage(1060160); // You are bleeding!
        BleedAttack.BeginBleed(m, Owner);

        base.Detonate(m);
    }
}
