using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Items;
using Server.Mobiles;

namespace Server.Multis;

[SerializationGenerator(2, false)]
public abstract partial class BaseCamp : BaseMulti
{
    private void MigrateFrom(V1Content content)
    {
        _items = content.Items;
        _mobiles = content.Mobiles;
        _decayTime = content.DecayTime;
    }

    [Tidy]
    [SerializableField(0, setter: "private")]
    private List<Item> _items;

    [Tidy]
    [SerializableField(1, setter: "private")]
    private List<Mobile> _mobiles;

    [AnchoredDateTime]
    [SerializableField(2, setter: "private")]
    private DateTime _decayTime;

    private TimeSpan _decayDelay;
    private Timer _decayTimer;
    private Timer _initTimer;

    public BaseCamp(int multiID) : base(multiID)
    {
        _items = new List<Item>();
        _mobiles = new List<Mobile>();
        _decayDelay = TimeSpan.FromMinutes(30.0);
        RefreshDecay(true);

        _initTimer = Timer.DelayCall(TimeSpan.Zero, CheckAddComponents);
    }

    public virtual int EventRange => 10;

    public TimeSpan DecayDelay
    {
        get => _decayDelay;
        set
        {
            _decayDelay = value;
            RefreshDecay(true);
        }
    }

    public override bool HandlesOnMovement => true;

    public void CheckAddComponents()
    {
        _initTimer = null;
        
        if (Deleted)
        {
            return;
        }

        AddComponents();
    }

    public virtual void AddComponents()
    {
    }

    /// <summary>A locked, trapped crate and a treasure chest at the corners of the camp.
    /// Lives here rather than on BrigandCamp because PrisonerCamp wants the same pair — which
    /// is where the original keeps it too.</summary>
    protected virtual void AddCampChests()
    {
        LockableContainer chest = Utility.Random(3) switch
        {
            0 => new MetalChest(),
            1 => new MetalGoldenChest(),
            _ => new WoodenChest()
        };

        chest.LiftOverride = true;

        TreasureMapChest.Fill(chest, 1);

        AddItem(chest, -2, -2, 0);

        LockableContainer crates = Utility.Random(4) switch
        {
            0 => new SmallCrate(),
            1 => new MediumCrate(),
            2 => new LargeCrate(),
            _ => new LockableBarrel()
        };

        crates.TrapType = TrapType.ExplosionTrap;
        crates.TrapPower = Utility.RandomMinMax(30, 40);
        crates.TrapLevel = 2;

        crates.RequiredSkill = 76;
        crates.LockLevel = 66;
        crates.MaxLockLevel = 116;
        crates.Locked = true;

        crates.DropItem(new Gold(Utility.RandomMinMax(100, 400)));
        crates.DropItem(new Arrow(10));
        crates.DropItem(new Bolt(10));

        crates.LiftOverride = true;

        crates.DropItem(
            Utility.Random(5) switch
            {
                0 => new LesserCurePotion(),
                1 => new LesserExplosionPotion(),
                2 => new LesserHealPotion(),
                3 => new LesserPoisonPotion(),
                _ => null // 4
            }
        );

        AddItem(crates, 2, 2, 0);
    }

    public virtual void RefreshDecay(bool setDecayTime)
    {
        if (Deleted)
        {
            return;
        }

        if (setDecayTime)
        {
            _decayTime = Core.Now + _decayDelay;
        }

        _decayTimer?.Stop();
        _decayTimer = Timer.DelayCall(_decayDelay, Delete);
    }

    public virtual void AddItem(Item item, int xOffset, int yOffset, int zOffset)
    {
        AddToItems(item);

        var zavg = Map.GetAverageZ(X + xOffset, Y + yOffset);
        item.MoveToWorld(new Point3D(X + xOffset, Y + yOffset, zavg + zOffset), Map);
    }

    public virtual void AddMobile(Mobile m, int wanderRange, int xOffset, int yOffset, int zOffset)
    {
        AddToMobiles(m);

        var zavg = Map.GetAverageZ(X + xOffset, Y + yOffset);
        var loc = new Point3D(X + xOffset, Y + yOffset, zavg + zOffset);

        if (m is BaseCreature bc)
        {
            bc.RangeHome = wanderRange;
            bc.Home = loc;
        }

        if (m is BaseVendor)
        {
            m.Direction = Direction.South;
        }

        m.MoveToWorld(loc, Map);
    }

    public virtual void OnEnter(Mobile m)
    {
        RefreshDecay(true);
    }

    public virtual void OnExit(Mobile m)
    {
        RefreshDecay(true);
    }

    public override void OnMovement(Mobile m, Point3D oldLocation)
    {
        var inOldRange = Utility.InRange(oldLocation, Location, EventRange);
        var inNewRange = Utility.InRange(m.Location, Location, EventRange);

        if (inNewRange && !inOldRange)
        {
            OnEnter(m);
        }
        else if (inOldRange && !inNewRange)
        {
            OnExit(m);
        }
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();

        for (var i = 0; i < _items.Count; ++i)
        {
            _items[i]?.Delete();
        }

        for (var i = 0; i < _mobiles.Count; ++i)
        {
            var mob = _mobiles[i];

            if (mob != null && (mob.CantWalk || (mob as BaseCreature)?.IsPrisoner == false))
            {
                mob.Delete();
            }
        }

        ClearItems();
        ClearMobiles();

        _decayTimer?.Stop();
        _decayTimer = null;
        
        _initTimer?.Stop();
        _initTimer = null;
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        _items = reader.ReadEntityList<Item>();
        _mobiles = reader.ReadEntityList<Mobile>();
        _decayTime = reader.ReadDeltaTime();
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        var remaining = _decayTime - Core.Now;
        
        if (remaining > TimeSpan.Zero)
        {
            _decayDelay = remaining;
            RefreshDecay(false);
        }
        else
        {
            Timer.DelayCall(TimeSpan.Zero, Delete);
            return;
        }
        
        _initTimer = Timer.DelayCall(TimeSpan.Zero, CheckAddComponents);
    }
}

[SerializationGenerator(0, false)]
public partial class LockableBarrel : LockableContainer
{
    [Constructible]
    public LockableBarrel() : base(0xE77)
    {
    }

    public override double DefaultWeight => 1.0;
}
