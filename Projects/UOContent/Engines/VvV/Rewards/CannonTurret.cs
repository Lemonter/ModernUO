using System;
using System.Collections.Generic;
using System.Linq;
using ModernUO.Serialization;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.VvV;

// Ported from real OSI/ServUO content (Scripts/Services/ViceVsVirtue/Items/Rewards/CannonTurret.cs).
[SerializationGenerator(0, false)]
public partial class CannonTurret : BaseAddon
{
    public const int ScanRange = 8;
    public const int ReloadDelay = 10;

    private bool _noShoot;

    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Mobile _owner;

    [SerializableField(1, fieldChanged: nameof(OnShotsRemainingChanged))]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _shotsRemaining;

    [SerializableField(2)]
    private CannonBase _base;

    [SerializableField(3)]
    private Item _turret;

    private DateTime _nextShot;

    public override BaseAddonDeed Deed => null;

    [Constructible]
    public CannonTurret() : this(null)
    {
    }

    public CannonTurret(Mobile m)
    {
        _owner = m;
        _shotsRemaining = 20;

        _base = new CannonBase(this);
        _base.MoveToWorld(Location, Map);

        var c = new LocalizedAddonComponent(16918, 1155505);
        AddComponent(c, 0, 0, 3);
        _turret = c;

        _nextShot = Core.Now;
    }

    private void OnShotsRemainingChanged(int oldValue, int newValue)
    {
        if (newValue <= 0)
        {
            Delete();
        }
    }

    public override void OnLocationChange(Point3D oldLocation)
    {
        base.OnLocationChange(oldLocation);

        if (Base is { Deleted: false })
        {
            Base.Location = Location;
        }
    }

    public override void OnMapChange()
    {
        base.OnMapChange();

        if (Base is { Deleted: false })
        {
            Base.Map = Map;
        }
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();

        if (Base is { Deleted: false })
        {
            Base.Delete();
        }
    }

    public void Scan()
    {
        if (Deleted || Map == null || ShotsRemaining <= 0 || _noShoot)
        {
            return;
        }

        var list = new List<Mobile>();

        foreach (var m in Map.GetMobilesInRange(Location, ScanRange))
        {
            if (Owner == null || (ViceVsVirtueSystem.IsEnemy(Owner, m) && m.InLOS(Location) && m is PlayerMobile && m.AccessLevel == AccessLevel.Player))
            {
                list.Add(m);
            }
        }

        Mobile target = null;
        double closest = ScanRange;

        foreach (var m in list.Where(mob => target == null || mob.GetDistanceToSqrt(target) < closest))
        {
            target = m;
            closest = m.GetDistanceToSqrt(target);
        }

        if (target != null)
        {
            AimAndShoot(target, (int)Math.Ceiling(closest));
        }
    }

    public void AimAndShoot(Mobile target, int range)
    {
        var d = Utility.GetDirection(Location, target.Location);

        Turret.ItemID = d switch
        {
            Direction.East or Direction.Down => 16921,
            Direction.South or Direction.Left => 16918,
            Direction.West or Direction.Up => 16919,
            _ => 16920
        };

        if (_nextShot > Core.Now)
        {
            return;
        }

        Timer.DelayCall(
            TimeSpan.FromMilliseconds(250),
            () =>
            {
                var p = new Point3D(X, Y, Z + 2);
                var map = Map;

                switch (Turret.ItemID)
                {
                    case 16920: p.Y--; break;
                    case 16921: p.X++; break;
                    case 16918: p.Y++; break;
                    case 16919: p.X--; break;
                }

                Effects.SendLocationEffect(p, map, 14120, 15, 10);
                Effects.PlaySound(p, map, 0x664);
            }
        );

        Timer.DelayCall(
            TimeSpan.FromMilliseconds(250 + 150 * range),
            () =>
            {
                Owner?.DoHarmful(target);

                AOS.Damage(target, Owner, Utility.RandomMinMax(75, 100), 100, 0, 0, 0, 0);

                Effects.SendLocationEffect(target.Location, target.Map, Utility.RandomBool() ? 14000 : 14013, 15, 10);
                Effects.PlaySound(target.Location, target.Map, 0x207);

                ShotsRemaining--;
            }
        );

        _nextShot = Core.Now + TimeSpan.FromSeconds(ReloadDelay);
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        _noShoot = true;
        Timer.DelayCall(
            TimeSpan.FromSeconds(10),
            () =>
            {
                if (Base != null && ViceVsVirtueSystem.Instance.Battle.OnGoing)
                {
                    Base.Turret = this;
                    ViceVsVirtueSystem.Instance.Battle.Turrets.Add(this);
                    _noShoot = false;
                    return;
                }

                Delete();
            }
        );
    }

    // ServUO's CannonBase is a `DamageableItem` (a destructible-prop framework with its own HP
    // bar) — that class doesn't exist in this codebase, so this is a plain Item instead; the
    // turret can no longer be destroyed by attacking its base, only via ShotsRemaining reaching 0.
    [SerializationGenerator(0, false)]
    public partial class CannonBase : Item
    {
        public override int LabelNumber => 1155505;

        [SerializableField(0)]
        private CannonTurret _turret;

        public CannonBase(CannonTurret turret) : base(1822)
        {
            Movable = false;
            _turret = turret;

            Name = "a cannon turret";
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();
            Turret?.Delete();
        }
    }
}

[SerializationGenerator(0, false)]
public partial class CannonTurretPlans : Item
{
    public override int LabelNumber => 1155503; // Plans for a Cannon Turret

    [Constructible]
    public CannonTurretPlans() : base(5630)
    {
    }

    public override void OnDoubleClick(Mobile m)
    {
        if (!IsChildOf(m.Backpack))
        {
            return;
        }

        var battle = ViceVsVirtueSystem.Instance.Battle;

        if (!ViceVsVirtueSystem.IsVvV(m))
        {
            m.SendLocalizedMessage(1155496); // This item can only be used by VvV participants!
        }
        else if (battle is not { OnGoing: true } || !battle.IsInActiveBattle(m))
        {
            m.SendLocalizedMessage(1155406); // This item can only be used in an active VvV battle region!
        }
        else if (battle.TurretCount > VvVBattle.MaxTurrets)
        {
            m.SendLocalizedMessage(1155502); // The turret limit for this battle has been reached!
        }
        else
        {
            var t = new CannonTurret(m);
            t.MoveToWorld(m.Location, m.Map);

            battle.Turrets.Add(t);

            Delete();
        }
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);
        list.Add(1154937); // vvv item
    }
}
