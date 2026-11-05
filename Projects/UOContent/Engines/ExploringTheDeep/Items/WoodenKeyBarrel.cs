using System;
using ModernUO.Serialization;
using Server.Collections;
using Server.Mobiles;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/ExploringTheDeep/Items/WoodenKeyBarrel.cs).
///
///     Two substitutions, both forced by APIs this codebase does not have:
///     <list type="bullet">
///         <item>
///             ServUO builds these on its <c>DamageableItem</c> framework (an <c>ItemLevel</c>, a hit point
///             pool and the combat plumbing that lets players attack items). That framework does not exist
///             here — the other ports in this repository that hit the same wall (see
///             <c>Items/Decorative/DesCityWall.cs</c>) settle for plain items too. So a barrel is broken
///             open by double-clicking it instead of by hitting it. Everything that happened on destruction
///             still happens: the key barrel yields the <see cref="LockerKey" /> and restarts its locker,
///             every other barrel rolls the barrel wrath, spawns sea creatures and leaves debris.
///         </item>
///         <item>
///             <c>BarrelExplodeWrathTimer</c> / <c>BarrelPoisonWrathTimer</c> are gone. In ServUO they
///             fired one zero-delay timer per tile of the blast, and each timer damaged every player within
///             four tiles of <em>its own</em> tile, so a single barrel could hit one player dozens of
///             times. The blast is now resolved once per affected player.
///         </item>
///     </list>
/// </summary>
[SerializationGenerator(0, false)]
public partial class WoodenKeyBarrel : Item
{
    [SerializableField(0, setter: "private")]
    private Parts _key;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private StorageLocker _storageLocker;

    [Constructible]
    public WoodenKeyBarrel() : this(Parts.None)
    {
    }

    [Constructible]
    public WoodenKeyBarrel(Parts key) : base(0x0FAE)
    {
        _key = key;
        Name = "бочка";
        Movable = false;
    }

    public override int LabelNumber => 1023703; // barrel

    public override void OnDoubleClick(Mobile from)
    {
        if (Deleted || from.Map != Map || !from.InRange(Location, 2))
        {
            return;
        }

        BreakOpen(from);
    }

    private void BreakOpen(Mobile from)
    {
        var location = Location;
        var map = Map;

        if (_key != Parts.None)
        {
            new LockerKey(_key).MoveToWorld(location, map);
            _storageLocker?.BeginRestart(TimeSpan.FromMinutes(10.0));
        }
        else
        {
            var debris = new WoodKeyDebris();
            debris.MoveToWorld(location, map);

            switch (Utility.Random(3))
            {
                case 0:
                    {
                        BarrelWrath.DoRandomWrath(this, from);
                        break;
                    }
                case 1:
                    {
                        BarrelPoisonWrath.DoRandomWrath(this, from);
                        break;
                    }
                case 2:
                    {
                        SpawnCreatures(location, map);
                        break;
                    }
            }

            if (Utility.RandomDouble() < 0.05)
            {
                new BarrelHoops().MoveToWorld(location, map);
            }

            if (Utility.RandomDouble() < 0.05)
            {
                new BarrelStaves().MoveToWorld(location, map);
            }

            if (Utility.RandomDouble() < 0.05)
            {
                new BarrelLid().MoveToWorld(location, map);
            }

            if (Utility.RandomDouble() < 0.05)
            {
                new CopperWire().MoveToWorld(location, map);
            }
        }

        Delete();
    }

    private static void SpawnCreatures(Point3D location, Map map)
    {
        BaseCreature spawn = Utility.Random(6) switch
        {
            0 => new SeaSnake(),
            1 => new ShipRat(),
            2 => new ShipBat(),
            3 => new ShipBat(),
            4 => new ShipRat(),
            _ => new SeaSnake()
        };

        spawn.MoveToWorld(location, map);
    }
}

/// <summary>
///     A barrel of the outer ring: breaking it open reveals a reinforced metal barrel underneath.
/// </summary>
[SerializationGenerator(0, false)]
public partial class WoodenToMetalBarrel : Item
{
    [SerializableField(0, setter: "private")]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private StorageLocker _storageLocker;

    [Constructible]
    public WoodenToMetalBarrel() : base(0x0FAE)
    {
        Name = "бочка";
        Movable = false;
    }

    [Constructible]
    public WoodenToMetalBarrel(StorageLocker item) : base(0x0FAE)
    {
        Name = "бочка";
        Movable = false;
        _storageLocker = item;
    }

    public override int LabelNumber => 1023703; // barrel

    public override void OnDoubleClick(Mobile from)
    {
        if (Deleted || from.Map != Map || !from.InRange(Location, 2))
        {
            return;
        }

        var barrel = new MetalBarrel();
        _storageLocker?.Barrels?.Add(barrel);

        barrel.MoveToWorld(Location, Map);
        Delete();
    }
}

[SerializationGenerator(0, false)]
public partial class MetalBarrel : Item
{
    [Constructible]
    public MetalBarrel() : base(0x0FAE)
    {
        Movable = false;
        Hue = 2301;
    }

    public override int LabelNumber => 1023703; // barrel
}

/// <summary>
///     The splintered remains left behind by a broken barrel; swept away after half an hour.
/// </summary>
[SerializationGenerator(0, false)]
public partial class WoodKeyDebris : Item
{
    private TimerExecutionToken _timer;

    [Constructible]
    public WoodKeyDebris() : base(0x0C2F)
    {
        Movable = false;
        Timer.StartTimer(TimeSpan.FromMinutes(30.0), Delete, out _timer);
    }

    public override void OnAfterDelete()
    {
        _timer.Cancel();
        base.OnAfterDelete();
    }
}

/// <summary>
///     The blast from a ruptured barrel. ServUO resolved this with a zero-delay timer per tile; as noted
///     on <see cref="WoodenKeyBarrel" />, that multiplied the damage by the number of tiles, so the blast
///     is resolved once per victim here.
/// </summary>
public static class BarrelWrath
{
    public static void DoRandomWrath(Item source, Mobile from)
    {
        var range = Utility.RandomMinMax(2, 4);
        var minDamage = 30 + Utility.Random(30) - range * 10;

        if (minDamage < 0)
        {
            minDamage = 1;
        }

        DoWrath(source, from, minDamage, minDamage + Utility.Random(100), range);
    }

    public static void DoWrath(Item source, Mobile from, int minDamage, int maxDamage, int range)
    {
        if (source.Deleted)
        {
            return;
        }

        Effects.SendLocationParticles(
            EffectItem.Create(source.Location, source.Map, EffectItem.DefaultDuration), 0x36BD, 9, 10, 5044
        );
        Effects.PlaySound(source.Location, source.Map, 0x307);

        BarrelWrathDamage.Damage(source, from, range, minDamage, maxDamage, 1154447, false);
    }
}

public static class MetalBarrelWrath
{
    public static void DoRandomWrath(Item source, Mobile from)
    {
        var range = Utility.RandomMinMax(4, 7);
        var minDamage = 60 + Utility.Random(60) - range * 10;

        if (minDamage < 0)
        {
            minDamage = 1;
        }

        DoWrath(source, from, minDamage, minDamage + Utility.Random(200), range);
    }

    public static void DoWrath(Item source, Mobile from, int minDamage, int maxDamage, int range)
    {
        if (source.Deleted)
        {
            return;
        }

        Effects.SendLocationParticles(
            EffectItem.Create(source.Location, source.Map, EffectItem.DefaultDuration), 0x36BD, 9, 10, 5044
        );
        Effects.PlaySound(source.Location, source.Map, 0x307);

        BarrelWrathDamage.Damage(source, from, range, minDamage, maxDamage, 1154447, false);
    }
}

public static class BarrelPoisonWrath
{
    public static void DoRandomWrath(Item source, Mobile from) => DoWrath(source, from, 0, 0, Utility.RandomMinMax(2, 4));

    public static void DoWrath(Item source, Mobile from, int minDamage, int maxDamage, int range)
    {
        if (source.Deleted)
        {
            return;
        }

        Effects.SendLocationParticles(
            EffectItem.Create(source.Location, source.Map, EffectItem.DefaultDuration), 0x376A, 9, 10, 5040
        );
        Effects.PlaySound(source.Location, source.Map, 0x474);

        BarrelWrathDamage.Damage(source, from, range, minDamage, maxDamage, 1154446, true);
    }
}

internal static class BarrelWrathDamage
{
    public static void Damage(
        Item source, Mobile from, int range, int minDamage, int maxDamage, int message, bool poison
    )
    {
        var map = source.Map;

        if (map == null || map == Map.Internal)
        {
            return;
        }

        // The player who broke the barrel is always in range, but the blast is centred on the barrel, not
        // on them, so it has to be resolved against the map rather than the breaker.
        using var victims = PooledRefList<Mobile>.Create();

        foreach (var m in map.GetMobilesInRange(source.Location, range))
        {
            if (m.Alive && m.Player && m.AccessLevel == AccessLevel.Player && !victims.Contains(m))
            {
                victims.Add(m);
            }
        }

        for (var i = 0; i < victims.Count; i++)
        {
            var m = victims[i];

            m.DoHarmful(m);
            m.PublicOverheadMessage(MessageType.Regular, 0x3B2, message);

            if (poison)
            {
                m.ApplyPoison(m, Poison.Deadly);
                m.PlaySound(0x474);
            }
            else
            {
                m.FixedParticles(0x376A, 1, 3, 5052, EffectLayer.Waist);
                m.Damage(Utility.RandomMinMax(minDamage, maxDamage), m);
                Effects.PlaySound(source.Location, map, 0x307);
            }
        }
    }
}
