using System;
using System.Collections.Generic;
using System.Linq;
using ModernUO.Serialization;
using Server.Collections;
using Server.ContextMenus;
using Server.Items;
using Server.Targeting;

namespace Server.Engines.VvV;

// Ported from real OSI/ServUO content (Scripts/Services/ViceVsVirtue/Items/Rewards/VvVTrapKit.cs).
[SerializationGenerator(0, false)]
public partial class VvVTrapKit : Item
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private DeploymentType _deploymentType;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private VvVTrapType _trapType;

    public override int LabelNumber => 1154944; // Trap Kit

    private static readonly Dictionary<Mobile, DateTime> _cooldown = new();

    [Constructible]
    public VvVTrapKit() : this(VvVTrapType.Explosion)
    {
    }

    [Constructible]
    public VvVTrapKit(VvVTrapType type) : base(7866)
    {
        _trapType = type;
        _deploymentType = DeploymentType.Proximaty;
    }

    public override void OnDoubleClick(Mobile from)
    {
        CheckCooldown();

        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042004); // That must be in your pack for you to use it
            return;
        }

        var sys = ViceVsVirtueSystem.Instance;

        if (sys == null)
        {
            return;
        }

        if (!ViceVsVirtueSystem.IsVvV(from))
        {
            from.SendLocalizedMessage(1155415); // Only participants in Vice vs Virtue may use this item.
        }
        else if (!sys.Battle.OnGoing || !from.Region.IsPartOf(sys.Battle.Region))
        {
            from.SendLocalizedMessage(1155406); // This item can only be used in an active VvV battle region!
        }
        else if (sys.Battle.TrapCount >= VvVBattle.MaxTraps)
        {
            from.SendLocalizedMessage(1155407); // The trap limit for this battle has been reached!
        }
        else if (_cooldown.ContainsKey(from))
        {
            from.SendLocalizedMessage(1155408); // You must wait a few moments before attempting to place another trap.
        }
        else
        {
            from.SendLocalizedMessage(1155409); // Where do you want to place the trap?
            from.BeginTarget(
                2,
                true,
                TargetFlags.None,
                (m, targeted) =>
                {
                    if (targeted is not IPoint3D p)
                    {
                        m.SendLocalizedMessage(1042261); // You cannot place the trap there.
                        return;
                    }

                    if (!sys.Battle.OnGoing || !m.Region.IsPartOf(sys.Battle.Region))
                    {
                        m.SendLocalizedMessage(1155406); // This item can only be used in an active VvV battle region!
                    }
                    else if (sys.Battle.Traps.Count >= VvVBattle.MaxTraps)
                    {
                        m.SendLocalizedMessage(1155407); // The trap limit for this battle has been reached!
                    }
                    else if (!from.InLOS(p))
                    {
                        m.SendLocalizedMessage(1042261); // You cannot place the trap there.
                    }
                    else
                    {
                        TryDeployTrap(m, new Point3D(p));
                    }
                }
            );
        }
    }

    public void TryDeployTrap(Mobile m, Point3D trapLocation)
    {
        VvVTrap trap = null;

        if (DeploymentType == DeploymentType.Tripwire)
        {
            m.SendLocalizedMessage(1155410); // Target the location to run the tripwire...
            m.BeginTarget(
                5,
                true,
                TargetFlags.None,
                (from, targeted) =>
                {
                    if (targeted is not IPoint3D p)
                    {
                        m.SendLocalizedMessage(1042261); // You cannot place the trap there.
                        return;
                    }

                    var point = new Point3D(p);

                    if (!Utility.InRange(point, trapLocation, 3) || point == trapLocation)
                    {
                        m.SendLocalizedMessage(1011577); // This is an invalid location.
                        return;
                    }

                    trap = ConstructTrap(m);

                    if (!trap.SetTripwire(this, trapLocation, point, m.Map))
                    {
                        trap.Delete();
                        m.SendLocalizedMessage(1042261); // You cannot place the trap there.
                        return;
                    }

                    m.PrivateOverheadMessage(MessageType.Regular, 1154, 1155411, m.NetState); // *You successfully lay the tripwire*

                    trap.MoveToWorld(trapLocation, m.Map);
                    Delete();

                    ViceVsVirtueSystem.Instance.Battle.Traps.Add(trap);
                    AddToCooldown(m);
                }
            );

            return;
        }

        m.PrivateOverheadMessage(MessageType.Regular, 1154, 1155412, m.NetState); // *You successfully set the trap*
        trap = ConstructTrap(m);

        trap.MoveToWorld(trapLocation, m.Map);
        Delete();

        ViceVsVirtueSystem.Instance.Battle.Traps.Add(trap);
        AddToCooldown(m);
    }

    private void AddToCooldown(Mobile m) => _cooldown[m] = Core.Now + TimeSpan.FromSeconds(30);

    private static void CheckCooldown()
    {
        foreach (var m in _cooldown.Keys.Where(mob => _cooldown[mob] < Core.Now).ToList())
        {
            _cooldown.Remove(m);
        }
    }

    public VvVTrap ConstructTrap(Mobile m) =>
        TrapType switch
        {
            VvVTrapType.Explosion => new VvVExplosionTrap(m, DeploymentType),
            VvVTrapType.Poison    => new VvVPoisonTrap(m, DeploymentType),
            VvVTrapType.Cold      => new VvVColdTrap(m, DeploymentType),
            VvVTrapType.Energy    => new VvVEnergyTrap(m, DeploymentType),
            VvVTrapType.Blade     => new VvVBladeTrap(m, DeploymentType),
            _                     => null
        };

    public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list) => list.Add(new InternalEntry(this, from));

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1154938, $"#{(int)DeploymentType}"); // Deployment Type: ~1_DEPLOYTYPE~
        list.Add(1154941, $"#{(int)TrapType}"); // Damage Type: ~1_DMGTYPE~
        list.Add(1154937); // VvV Item
    }

    private class InternalEntry : ContextMenuEntry
    {
        public VvVTrapKit Deed { get; }
        public Mobile Clicker { get; }

        public InternalEntry(VvVTrapKit deed, Mobile m) : base(1155514, -1)
        {
            Deed = deed;
            Clicker = m;

            if (!Deed.IsChildOf(m.Backpack))
            {
                Enabled = false;
            }
        }

        public override void OnClick(Mobile from, IEntity target)
        {
            Deed.DeploymentType = Deed.DeploymentType == DeploymentType.Proximaty
                ? DeploymentType.Tripwire
                : DeploymentType.Proximaty;

            Deed.InvalidateProperties();

            Clicker.PrivateOverheadMessage(MessageType.Regular, 1154, 1155515, Clicker.NetState); // *You adjust the deployment mechanism*
        }
    }
}
