using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Commands;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.SeasonalEvents;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/KrampusEncounter/KrampusEncounter.cs).
///     Per the season-port rules this controller is <b>not auto-started</b>: <see cref="Enabled" /> defaults to
///     false. ServUO's trade-based trigger (<c>CityLoyalty.CityTradeSystem.HasTrade</c> +
///     <c>TradeEntry.Distance</c>) is dropped — the city loyalty/trade system does not exist here — so the encounter
///     is spawned by the <c>[KrampusEncounter</c> command or <see cref="SpawnEncounter" />. The location line
///     (ServUO's <c>WorldLocationInfo</c>) is replaced with plain coordinates.
/// </summary>
public class KrampusEncounter
{
    public const int MinComplete = 20;

    private static KrampusEncounterPersistence _persistence;

    /// <summary>Автозапуск запрещён: включается вручную.</summary>
    public static bool Enabled { get; set; }

    public static KrampusEncounter Encounter { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int TotalTradesComplete { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Wave => Math.Clamp((int)(TotalTradesComplete / 4.1), 1, 6);

    [CommandProperty(AccessLevel.GameMaster)]
    public Krampus Krampus { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public Point3D SpawnLocation { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public Map SpawnMap { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool KrampusSpawning => SpawnMap != null && SpawnLocation != Point3D.Zero;

    public Dictionary<PlayerMobile, int> CompleteTable { get; } = new();

    public static void Configure()
    {
        _persistence = new KrampusEncounterPersistence();
    }

    public static void Initialize()
    {
        CommandSystem.Register("KrampusEncounter", AccessLevel.Administrator, e =>
        {
            if (Encounter == null)
            {
                Encounter = new KrampusEncounter();
            }

            e.Mobile.SendGump(new Gumps.PropertiesGump(e.Mobile, Encounter));
        });

        CommandSystem.Register("KrampusSpawn", AccessLevel.Administrator, e =>
        {
            if (Encounter == null || Encounter.Krampus != null)
            {
                e.Mobile.SendMessage("Встреча с Крампусом уже идёт.");
                return;
            }

            Encounter.SpawnEncounter(e.Mobile);
        });
    }

    public static bool KrampusSpawned() =>
        Enabled && Encounter?.Krampus is { Deleted: false };

    public int GetOrdersComplete(PlayerMobile pm) =>
        pm != null && CompleteTable.TryGetValue(pm, out var count) ? count : 0;

    public void AddOrderComplete(PlayerMobile pm)
    {
        if (pm == null)
        {
            return;
        }

        CompleteTable[pm] = GetOrdersComplete(pm) + 1;
        TotalTradesComplete++;
    }

    public void SpawnEncounter(Mobile from)
    {
        if (from?.Map == null)
        {
            return;
        }

        SpawnMap = from.Map;
        var p = from.Location;

        for (var i = 0; i < 25; i++)
        {
            var x = p.X + Utility.RandomMinMax(-3, 3);
            var y = p.Y + Utility.RandomMinMax(-3, 3);
            var z = from.Map.GetAverageZ(x, y);

            if (from.Map.CanSpawnMobile(x, y, z))
            {
                p = new Point3D(x, y, z);
                break;
            }
        }

        SpawnLocation = p;
        FinishSpawn();
    }

    private void FinishSpawn()
    {
        Krampus = new Krampus
        {
            SpawnLocation = SpawnLocation,
            Home = SpawnLocation,
            RangeHome = 5
        };

        Krampus.MoveToWorld(SpawnLocation, SpawnMap);
        Krampus.Summon(Krampus, true);

        var rec = new Rectangle2D(SpawnLocation.X - 10, SpawnLocation.Y - 10, 20, 20);

        for (var i = 0; i < 2; i++)
        {
            var drake = new FrostDrake();
            var p = SpawnLocation;

            for (var j = 0; j < 10; j++)
            {
                p = Utility.RandomPointIn(rec, SpawnMap);

                if (SpawnMap.CanSpawnMobile(p.X, p.Y, p.Z))
                {
                    break;
                }
            }

            drake.MoveToWorld(p, SpawnMap);
            drake.Home = p;
            drake.RangeHome = 15;
        }

        SpawnLocation = Point3D.Zero;
        SpawnMap = null;
    }

    public void OnKrampusKilled()
    {
        Krampus = null;

        CompleteTable.Clear();
        TotalTradesComplete = 0;

        if (!Enabled)
        {
            Encounter = null;
        }
    }

    public void Serialize(IGenericWriter writer)
    {
        writer.Write(0);

        writer.Write(Krampus);
        writer.Write(SpawnLocation);
        writer.Write(SpawnMap);
        writer.Write(TotalTradesComplete);
        writer.Write(CompleteTable.Count);

        foreach (var (pm, count) in CompleteTable)
        {
            writer.Write(pm);
            writer.Write(count);
        }
    }

    public void Deserialize(IGenericReader reader)
    {
        reader.ReadInt();

        Krampus = reader.ReadEntity<Mobile>() as Krampus;
        SpawnLocation = reader.ReadPoint3D();
        SpawnMap = reader.ReadMap();
        TotalTradesComplete = reader.ReadInt();

        var count = reader.ReadInt();

        for (var i = 0; i < count; i++)
        {
            var pm = reader.ReadEntity<Mobile>() as PlayerMobile;
            var orders = reader.ReadInt();

            if (pm != null)
            {
                CompleteTable[pm] = orders;
            }
        }
    }
}

public class KrampusEncounterPersistence : GenericPersistence
{
    public KrampusEncounterPersistence() : base("KrampusEncounter", 10)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
        writer.Write(0);

        if (KrampusEncounter.Encounter != null)
        {
            writer.Write(1);
            KrampusEncounter.Encounter.Serialize(writer);
        }
        else
        {
            writer.Write(0);
        }
    }

    public override void Deserialize(IGenericReader reader)
    {
        reader.ReadInt();

        if (reader.ReadInt() == 1)
        {
            KrampusEncounter.Encounter = new KrampusEncounter();
            KrampusEncounter.Encounter.Deserialize(reader);
        }
    }
}

