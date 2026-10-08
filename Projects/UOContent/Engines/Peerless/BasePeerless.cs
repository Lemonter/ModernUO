using System;
using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Services/Peerless/BasePeerless.cs). Base for the six
/// ML peerless bosses and their Stygian Abyss successors — the creature half of the
/// encounter, with PeerlessAltar owning the room, the timer and the rewards.
///
/// Carries three optional boss behaviours the subclasses opt into: a ring of fire while the
/// boss is still healthy, waves of helpers as its health drops, and the altar callback on
/// death. None of them fire unless the subclass turns them on.</summary>
[SerializationGenerator(0, false)]
public abstract partial class BasePeerless : BaseCreature
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private PeerlessAltar _altar;

    private long _nextFireRing;
    private int _currentWave;

    public BasePeerless(
        AIType aiType, FightMode fightMode = FightMode.Closest, int rangePerception = 10,
        int rangeFight = 1
    ) : base(aiType, fightMode, rangePerception, rangeFight)
    {
        _nextFireRing = Core.TickCount + 10000;
        _currentWave = MaxHelpersWaves;
    }

    public override bool CanBeParagon => false;
    public override bool Unprovokable => true;

    public virtual bool DropPrimer => true;
    public virtual bool GiveMLSpecial => true;
    public virtual double ChangeCombatant => 0.3;

    public virtual bool HasFireRing => false;
    public virtual double FireRingChance => 1.0;

    public virtual bool CanSpawnHelpers => false;
    public virtual int MaxHelpersWaves => 0;
    public virtual double SpawnHelpersChance => 0.05;

    [CommandProperty(AccessLevel.GameMaster)]
    public int CurrentWave
    {
        get => _currentWave;
        set => _currentWave = value;
    }

    public bool AllHelpersDead => _altar?.AllHelpersDead() != false;

    public override void OnThink()
    {
        base.OnThink();

        if (HasFireRing && Combatant != null && Alive && Hits > HitsMax * 0.8 &&
            _nextFireRing <= Core.TickCount && FireRingChance > Utility.RandomDouble())
        {
            FireRing();
        }

        if (CanSpawnHelpers && CanSpawnWave())
        {
            SpawnHelpers();
        }
    }

    public override void OnDeath(Container c)
    {
        base.OnDeath(c);

        _altar?.OnPeerlessDeath();
    }

    /// <summary>A wave is due once the boss's remaining health has dropped past the fraction
    /// of waves still unspent — so the waves come out evenly across the fight rather than all
    /// at the end.</summary>
    public virtual bool CanSpawnWave()
    {
        if (MaxHelpersWaves <= 0 || _currentWave <= 0)
        {
            return false;
        }

        if ((double)Hits / HitsMax > (double)_currentWave / MaxHelpersWaves)
        {
            return false;
        }

        if (SpawnHelpersChance < Utility.RandomDouble())
        {
            return false;
        }

        _currentWave--;
        return true;
    }

    public virtual void SpawnHelpers()
    {
    }

    public void SpawnHelper(BaseCreature helper, int x, int y, int z) =>
        SpawnHelper(helper, new Point3D(x, y, z));

    public void SpawnHelper(BaseCreature helper, Point3D location)
    {
        helper.Home = location;
        helper.RangeHome = 4;

        _altar?.AddHelper(helper);

        helper.MoveToWorld(location, Map);
    }

    public void SpawnHelper(BaseCreature helper, int range)
    {
        var map = Map;

        if (map == null)
        {
            helper.Delete();
            return;
        }

        var x = X + Utility.RandomMinMax(-range, range);
        var y = Y + Utility.RandomMinMax(-range, range);
        var z = map.GetAverageZ(x, y);

        SpawnHelper(helper, new Point3D(x, y, z));
    }

    /// <summary>A cross of flame around the boss, north-south and east-west, on a ten second
    /// cooldown.</summary>
    public virtual void FireRing()
    {
        var map = Map;

        if (map == null)
        {
            return;
        }

        for (var i = -2; i <= 2; i++)
        {
            if (i == 0)
            {
                continue;
            }

            var north = new Point3D(X, Y + i, Z);
            var east = new Point3D(X + i, Y, Z);

            Effects.SendLocationEffect(north, map, 0x3709, 30, 10, 0, 0);
            Effects.SendLocationEffect(east, map, 0x3709, 30, 10, 0, 0);
        }

        Effects.PlaySound(Location, map, 0x225);

        _nextFireRing = Core.TickCount + 10000;
    }
}
