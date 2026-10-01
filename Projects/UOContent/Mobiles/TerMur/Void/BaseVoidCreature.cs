using System;
using ModernUO.Serialization;
using Server.Collections;
using Server.Engines.Spawners;
using Server.Items;

namespace Server.Mobiles;

public enum VoidEvolution
{
    None = 0,
    Killing = 1,
    Grouping = 2,
    Survival = 3
}

/// <summary>The Void creatures of Ter Mur, ported from ServUO
/// (Scripts/Mobiles/Void Creatures/BaseVoidCreature.cs). Their point is that they do not stay
/// what they are: left alone long enough, or gathered with their own kind, a void creature
/// replaces itself with the next thing along one of three evolution lines.
///
/// Three lines, three stages each:
/// killing → Betballem, Ballem, Usagralem Ballem;
/// grouping → Anlorzen, Anlorlem, Anlorvaglem;
/// survival → Anzuanord, Relanord, Vasanord.
///
/// Korpre is the stage-zero creature everything starts as. Which line a mutation takes is
/// decided the first time: grouped if enough of its own kind are within twelve tiles, survival
/// otherwise; after that the creature's own line carries it the rest of the way.
///
/// Dropped from the original: RemoveVoidSpawners, a one-time save migration that walked every
/// item in the world at load to reset old spawners (and printed to the console, which this
/// codebase forbids) — saves here are disposable, so there is nothing to migrate. The original's
/// Delete override, which hands the spawner its replacement so the slot is not double-counted,
/// is kept and done against this codebase's own spawner interface instead of XmlSpawner's
/// internals, so it works for native JSON spawners too.</summary>
[SerializationGenerator(0, false)]
public abstract partial class BaseVoidCreature : BaseCreature
{
    private static readonly Type[][] EvolutionCycle =
    [
        [typeof(Betballem), typeof(Ballem), typeof(UsagralemBallem)],
        [typeof(Anlorzen), typeof(Anlorlem), typeof(Anlorvaglem)],
        [typeof(Anzuanord), typeof(Relanord), typeof(Vasanord)]
    ];

    private BaseCreature _mutateTo;

    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private DateTime _nextMutate;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _buddyMutate;

    public BaseVoidCreature(
        AIType aiType, FightMode fightMode = FightMode.Closest, int rangePerception = 10, int rangeFight = 1
    ) : base(aiType, FightMode.Good, rangePerception, rangeFight)
    {
        _nextMutate = Core.Now + TimeSpan.FromMinutes(MutateCheck);
        _buddyMutate = true;
    }

    /// <summary>Half an hour to two hours before a lone creature evolves on its own.</summary>
    public static int MutateCheck => Utility.RandomMinMax(30, 120);

    public virtual int GroupAmount => 2;
    public virtual VoidEvolution Evolution => VoidEvolution.None;
    public virtual int Stage => 0;

    public override bool PlayerRangeSensitive => Evolution != VoidEvolution.Killing && Stage < 3;
    public override bool AlwaysMurderer => true;

    public override void OnThink()
    {
        base.OnThink();

        if (Stage >= 3 || _nextMutate > Core.Now)
        {
            return;
        }

        if (!MutateGrouped() && Alive && !Deleted)
        {
            Mutate(VoidEvolution.Survival);
        }
    }

    /// <summary>If enough of its own kind are nearby, they all evolve together.</summary>
    public bool MutateGrouped()
    {
        if (!_buddyMutate || Map == null)
        {
            return false;
        }

        using var buddies = PooledRefList<BaseVoidCreature>.Create();

        foreach (var m in Map.GetMobilesInRange<BaseVoidCreature>(Location, 12))
        {
            if (m != this && m.Alive && !m.Deleted && m.BuddyMutate && IsEvolutionType(m))
            {
                buddies.Add(m);
            }
        }

        if (buddies.Count < GroupAmount)
        {
            return false;
        }

        Mutate(VoidEvolution.Grouping);

        for (var i = 0; i < buddies.Count; i++)
        {
            buddies[i].Mutate(VoidEvolution.Grouping);
        }

        return true;
    }

    /// <summary>Before the first mutation only identical creatures count as kin; afterwards any
    /// void creature does.</summary>
    public bool IsEvolutionType(Mobile from) => Stage != 0 || from.GetType() == GetType();

    public void Mutate(VoidEvolution evolution)
    {
        if (!Alive || Deleted || Stage == 3 || Map == null)
        {
            return;
        }

        var evo = Stage > 0 ? Evolution : evolution;

        if (evo == VoidEvolution.None)
        {
            return;
        }

        if (Utility.RandomDouble() < 0.05)
        {
            SpawnOrtanords();
        }

        if (EvolutionCycle[(int)evo - 1][Stage].CreateInstance<BaseCreature>() is not { } bc)
        {
            return;
        }

        _mutateTo = bc;

        bc.MoveToWorld(Location, Map);
        bc.Home = Home;
        bc.RangeHome = RangeHome;

        if (Utility.RandomDouble() < 0.05)
        {
            SpawnOrtanords();
        }

        if (bc is BaseVoidCreature void_)
        {
            void_.BuddyMutate = _buddyMutate;
        }

        Delete();
    }

    /// <summary>A wisp of the Abyss tears loose when something evolves.</summary>
    public void SpawnOrtanords()
    {
        var map = Map;

        if (map == null)
        {
            return;
        }

        var spawnLoc = Location;

        for (var i = 0; i < 25; i++)
        {
            var x = Utility.RandomMinMax(X - 5, X + 5);
            var y = Utility.RandomMinMax(Y - 5, Y + 5);
            var p = new Point3D(x, y, map.GetAverageZ(x, y));

            if (map.CanSpawnMobile(p))
            {
                spawnLoc = p;
                break;
            }
        }

        var ortanord = new Ortanord();
        ortanord.MoveToWorld(spawnLoc, map);
        ortanord.BoltEffect(0);
    }

    public override void OnDeath(Container c)
    {
        base.OnDeath(c);

        if (Stage > 0 && Utility.RandomDouble() < 0.02 * (Stage + 3))
        {
            c.DropItem(new VoidEssence());
        }

        if (Stage == 3 && Utility.RandomDouble() < 0.12)
        {
            c.DropItem(new VoidCore());
        }
    }

    /// <summary>When a creature evolves it is deleted and its successor takes its place; hand
    /// the spawner the successor so the slot isn't freed and immediately refilled with another
    /// stage-zero creature.</summary>
    public override void Delete()
    {
        if (_mutateTo != null && Spawner is BaseSpawner spawner &&
            spawner.Spawned?.Remove(this, out var entry) == true)
        {
            entry?.RemoveFromSpawned(this);

            spawner.Spawned[_mutateTo] = entry;
            entry?.AddToSpawned(_mutateTo);

            _mutateTo.Spawner = spawner;
            Spawner = null;
        }

        base.Delete();
    }
}
