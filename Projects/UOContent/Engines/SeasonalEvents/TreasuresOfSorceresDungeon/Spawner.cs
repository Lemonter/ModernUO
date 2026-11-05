using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.SeasonalEvents;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfSorceresDungeon/Spawner.cs).
///     ServUO drove this from <c>BaseGump</c> and <c>Map.GetRandomSpawnPoint(Rectangle2D)</c>; this codebase has
///     neither, so the gump is a plain <see cref="Gumps.Gump" /> with a static <c>DisplayTo</c>, and spawn points are
///     picked with the same manual rectangle roll the Blackthorn invasion port uses. It is <b>not</b> built on the
///     local <c>ISpawner</c> contract: ServUO's spawner is a standalone seasonal manager, not a spawner item, so it
///     stays a plain controller with its persistence owned by <see cref="SorcerersDungeonData" />.
/// </summary>
public class TOSDSpawner
{
    public static TOSDSpawner Instance { get; set; }

    public BaseCreature Boss { get; set; }

    public int Index { get; set; }

    public int KillCount { get; set; }

    public List<BaseCreature> Spawn { get; set; }

    public bool Spawning { get; set; }

    public List<TOSDSpawnEntry> Entries { get; }

    private TimerExecutionToken _timerToken;

    public TOSDSpawner(bool activate = true)
    {
        Instance = this;

        Entries = new List<TOSDSpawnEntry>();
        Spawn = new List<BaseCreature>();

        BuildEntries();

        if (activate)
        {
            Activate();
        }
    }

    public static void Configure()
    {
        CommandSystem.Register("TOSDSpawner", AccessLevel.Administrator, TOSDSpawner_OnCommand);
    }

    private static void TOSDSpawner_OnCommand(CommandEventArgs e)
    {
        if (Instance != null)
        {
            TOSDSpawnerGump.DisplayTo((PlayerMobile)e.Mobile);
        }
        else
        {
            e.Mobile.SendMessage("Этот спавнер сейчас не активен. Включите Treasures of Sorcerer's Dungeon.");
        }
    }

    private void Activate()
    {
        foreach (var entry in Entries)
        {
            foreach (var spawner in Map.Ilshenar.GetItemsInBounds<XmlSpawner>(entry.SpawnArea))
            {
                spawner.DoReset = true;
            }
        }
    }

    public void Deactivate()
    {
        foreach (var entry in Entries)
        {
            foreach (var spawner in Map.Ilshenar.GetItemsInBounds<XmlSpawner>(entry.SpawnArea))
            {
                spawner.DoRespawn = true;
            }
        }

        EndTimer();

        foreach (var bc in Spawn)
        {
            bc.Delete();
        }

        Spawn.Clear();
        Boss = null;

        Instance = null;
    }

    public void BeginTimer()
    {
        EndTimer();

        Timer.StartTimer(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), OnTick, out _timerToken);
    }

    private void EndTimer()
    {
        _timerToken.Cancel();
    }

    private void OnTick()
    {
        if (Spawning)
        {
            return;
        }

        if (Boss != null && Boss.Deleted)
        {
            if (Index == Entries.Count - 1)
            {
                Index = 0;
            }
            else
            {
                Index++;
            }

            Reset();
        }
        else if (Boss == null && KillCount >= Entries[Index].ToKill)
        {
            DoSpawn(Entries[Index].Boss, true);
        }
        else if (Spawn.Count < Entries[Index].MaxSpawn && (Boss == null || Entries[Index].SpawnArea.Contains(Boss.Location)))
        {
            Spawning = true;

            var entry = Entries[Index];

            Timer.StartTimer(
                TimeSpan.FromSeconds(Utility.RandomMinMax(3, 8)),
                () =>
                {
                    DoSpawn(entry.Spawn[Utility.Random(entry.Spawn.Length)], false);

                    Spawning = false;
                }
            );
        }
    }

    public void DoSpawn(Type t, bool boss)
    {
        if (Activator.CreateInstance(t) is not BaseCreature spawn)
        {
            return;
        }

        for (var i = 0; i < 20; i++)
        {
            var p = GetRandomSpawnPoint(Entries[Index].SpawnArea);

            if (!Map.Ilshenar.CanSpawnMobile(p))
            {
                continue;
            }

            spawn.MoveToWorld(p, Map.Ilshenar);

            if (boss)
            {
                Boss = spawn;
            }
            else
            {
                Spawn.Add(spawn);
            }

            return;
        }

        spawn.Delete();
    }

    private static Point3D GetRandomSpawnPoint(Rectangle2D rec)
    {
        var x = Utility.RandomMinMax(rec.X, rec.X + rec.Width - 1);
        var y = Utility.RandomMinMax(rec.Y, rec.Y + rec.Height - 1);

        return new Point3D(x, y, Map.Ilshenar.GetAverageZ(x, y));
    }

    public void OnCreatureDeath(BaseCreature bc)
    {
        if (Spawn.Remove(bc))
        {
            KillCount++;
        }
    }

    public void Reset()
    {
        Spawn.Clear();
        Boss = null;
        Spawning = false;
        KillCount = 0;

        EndTimer();

        Timer.StartTimer(TimeSpan.FromMinutes(Utility.RandomMinMax(1, 3)), BeginTimer);
    }

    public void Serialize(IGenericWriter writer)
    {
        writer.Write(0);

        writer.Write(Boss);
        writer.Write(KillCount);
        writer.Write(Index);

        writer.Write(Spawn.Count);

        foreach (var bc in Spawn)
        {
            writer.Write(bc);
        }
    }

    public void Deserialize(IGenericReader reader)
    {
        reader.ReadInt();

        Boss = reader.ReadEntity<BaseCreature>();
        KillCount = reader.ReadInt();
        Index = reader.ReadInt();

        var count = reader.ReadInt();

        for (var i = 0; i < count; i++)
        {
            var bc = reader.ReadEntity<BaseCreature>();

            if (bc != null)
            {
                Spawn.Add(bc);
            }
        }
    }

    private void BuildEntries()
    {
        var minions = new[] { typeof(Zombie), typeof(Skeleton), typeof(Gargoyle), typeof(Lich), typeof(LichLord) };

        Entries.Add(new TOSDSpawnEntry(typeof(NightmareFairy), minions, new Rectangle2D(327, 26, 29, 32), 80, 15));
        Entries.Add(new TOSDSpawnEntry(typeof(Server.Mobiles.JackInTheBox), minions, new Rectangle2D(296, 10, 17, 26), 80, 15));
        Entries.Add(new TOSDSpawnEntry(typeof(HeadlessElf), minions, new Rectangle2D(271, 4, 20, 33), 80, 15));
        Entries.Add(new TOSDSpawnEntry(typeof(AbominableSnowman), minions, new Rectangle2D(227, 39, 21, 19), 80, 15));
        Entries.Add(new TOSDSpawnEntry(typeof(TwistedHolidayTree), minions, new Rectangle2D(251, 68, 25, 32), 80, 15));
        Entries.Add(new TOSDSpawnEntry(typeof(RabidReindeer), minions, new Rectangle2D(144, 5, 23, 19), 80, 15));
        Entries.Add(new TOSDSpawnEntry(typeof(GarishGingerman), minions, new Rectangle2D(60, 53, 13, 34), 80, 15));
        Entries.Add(new TOSDSpawnEntry(typeof(StockingSerpent), minions, new Rectangle2D(152, 48, 16, 23), 80, 15));
        Entries.Add(new TOSDSpawnEntry(typeof(JackThePumpkinKing), minions, new Rectangle2D(291, 73, 37, 36), 80, 15));
    }
}

public class TOSDSpawnEntry
{
    public Type Boss { get; }
    public Type[] Spawn { get; }
    public Rectangle2D SpawnArea { get; }
    public int ToKill { get; }
    public int MaxSpawn { get; }

    public TOSDSpawnEntry(Type boss, Type[] spawn, Rectangle2D area, int toKill, int maxSpawn)
    {
        Boss = boss;
        Spawn = spawn;
        SpawnArea = area;
        ToKill = toKill;
        MaxSpawn = maxSpawn;
    }
}

/// <summary>Ported from ServUO's <c>TOSDSpawnerGump</c>, rebuilt as a plain <see cref="Gumps.Gump" />.</summary>
public class TOSDSpawnerGump : Gumps.Gump
{
    private readonly PlayerMobile _user;

    private TOSDSpawnerGump(PlayerMobile pm) : base(50, 50)
    {
        _user = pm;

        AddBackground(0, 0, 500, 300, 9300);
        AddHtml(0, 10, 500, 20, "<center>Спавнер Treasures of Sorcerer's Dungeon</center>", false, false);

        var spawner = TOSDSpawner.Instance;

        if (spawner == null)
        {
            AddHtml(10, 40, 150, 20, "Спавнер отключён", false, false);
            return;
        }

        AddLabel(10, 40, 0, "Go");
        AddLabel(40, 40, 0, "Босс");
        AddLabel(240, 40, 0, "Сейчас");
        AddLabel(320, 40, 0, "Максимум");
        AddLabel(400, 40, 0, "Убито");

        var y = 60;

        for (var i = 0; i < spawner.Entries.Count; i++)
        {
            var entry = spawner.Entries[i];
            var hue = i == spawner.Index ? "green" : "red";

            AddButton(7, y, 1531, 1532, i + 100, GumpButtonType.Reply, 0);
            AddHtml(40, y, 200, 20, $"<basefont color={hue}>{entry.Boss.Name}</basefont>", false, false);
            AddHtml(320, y, 80, 20, $"<basefont color={hue}>{entry.MaxSpawn}</basefont>", false, false);

            if (hue == "green")
            {
                AddHtml(240, y, 80, 20, $"<basefont color={hue}>{spawner.Spawn.Count}</basefont>", false, false);
                AddHtml(400, y, 80, 20, $"<basefont color={hue}>{spawner.KillCount}</basefont>", false, false);
            }
            else
            {
                AddHtml(240, y, 80, 20, $"<basefont color={hue}>0</basefont>", false, false);
                AddHtml(400, y, 80, 20, $"<basefont color={hue}>0</basefont>", false, false);
            }

            y += 22;
        }
    }

    public static void DisplayTo(PlayerMobile pm)
    {
        if (pm?.NetState == null)
        {
            return;
        }

        pm.SendGump(new TOSDSpawnerGump(pm));
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (info.ButtonID <= 0)
        {
            return;
        }

        var spawner = TOSDSpawner.Instance;

        if (spawner == null)
        {
            return;
        }

        var id = info.ButtonID - 100;

        if (id < 0 || id >= spawner.Entries.Count)
        {
            return;
        }

        var entry = spawner.Entries[id];

        for (var i = 0; i < 20; i++)
        {
            var x = Utility.RandomMinMax(entry.SpawnArea.X, entry.SpawnArea.X + entry.SpawnArea.Width - 1);
            var y = Utility.RandomMinMax(entry.SpawnArea.Y, entry.SpawnArea.Y + entry.SpawnArea.Height - 1);
            var p = new Point3D(x, y, Map.Ilshenar.GetAverageZ(x, y));

            if (!Map.Ilshenar.CanSpawnMobile(p))
            {
                continue;
            }

            _user.MoveToWorld(p, Map.Ilshenar);
            break;
        }

        DisplayTo(_user);
    }
}
