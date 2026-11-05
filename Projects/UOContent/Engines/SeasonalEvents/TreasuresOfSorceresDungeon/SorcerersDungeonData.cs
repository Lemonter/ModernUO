using System;
using System.Collections.Generic;
using ModernUO.CodeGeneratedEvents;
using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Engines.SeasonalEvents;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfSorceresDungeon/SorcerersDungeonData.cs).
///     ServUO's <c>PointsSystem</c> base does not exist here, so this is a plain static store with its own
///     <see cref="Enabled" /> switch (default false). Dropped: the reforging drop (<c>RunicReforging</c> does not
///     exist — a plain random magic item is dropped instead, the same substitution the Doom port uses). The seasonal
///     spawner's state is persisted through <see cref="DataSource" /> here, where ServUO kept it inside the points
///     system.
/// </summary>
public static class SorcerersDungeonData
{
    private static readonly Dictionary<Mobile, int> _dungeonPoints = new();

    private static SorcerersDungeonPersistence _persistence;

    /// <summary>Сезонный переключатель Treasures of Sorcerer's Dungeon. По умолчанию выключен.</summary>
    public static bool Enabled { get; set; }

    public static bool InSeason => Enabled;

    public static void Configure()
    {
        _persistence = new SorcerersDungeonPersistence();
    }

    [OnEvent(nameof(PlayerMobile.PlayerDeletedEvent))]
    public static void OnPlayerDeleted(Mobile m) => _dungeonPoints.Remove(m);

    [OnEvent(nameof(CreatureEvents.CreatureDeathEvent))]
    public static void OnCreatureDeath(BaseCreature bc) => AwardKill(bc, bc.LastKiller);

    public static int GetPoints(Mobile m) => m != null && _dungeonPoints.TryGetValue(m, out var points) ? points : 0;

    public static void AwardPoints(Mobile m, int amount)
    {
        if (m == null || amount <= 0)
        {
            return;
        }

        _dungeonPoints[m] = GetPoints(m) + amount;
    }

    public static bool DeductPoints(Mobile m, int amount)
    {
        if (m == null || amount <= 0 || !_dungeonPoints.TryGetValue(m, out var current))
        {
            return false;
        }

        var remaining = current - amount;

        if (remaining > 0)
        {
            _dungeonPoints[m] = remaining;
        }
        else
        {
            _dungeonPoints.Remove(m);
        }

        return true;
    }

    /// <summary>ServUO's <c>PointsSystem.SorcerersDungeon.ProcessKill</c>.</summary>
    public static void AwardKill(BaseCreature victim, Mobile damager)
    {
        if (victim == null)
        {
            return;
        }

        TOSDSpawner.Instance?.OnCreatureDeath(victim);

        if (!Enabled || damager is not PlayerMobile pm || !damager.Alive || damager.Deleted)
        {
            return;
        }

        if (victim.Controlled || victim.Summoned || victim.Region?.IsPartOf("Sorcerer's Dungeon") != true)
        {
            return;
        }

        var fame = victim.Fame / 4;
        var luck = Math.Max(0, pm.Luck);

        AwardPoints(pm, (int)(fame * (1 + Math.Sqrt(luck) / 100)));

        var points = GetPoints(pm);

        const double a = 0.000863316841;
        const double b = 0.00000425531915;

        if (a * Math.Pow(10, b * points) <= Utility.RandomDouble())
        {
            return;
        }

        var item = Loot.RandomArmorOrShieldOrWeaponOrJewelry();

        if (item == null)
        {
            return;
        }

        damager.PlaySound(0x5B4);
        damager.SendLocalizedMessage(1157613); // You notice some of your fallen foes' equipment to be of enchanted origin and decide it may be of some value...

        if (!damager.PlaceInBackpack(item))
        {
            if (damager.BankBox?.TryDropItem(damager, item, false) == true)
            {
                damager.SendLocalizedMessage(1079730); // The item has been placed into your bank box.
            }
            else
            {
                damager.SendLocalizedMessage(1072523); // You find an artifact, but your backpack and bank are too full to hold it.
                item.MoveToWorld(damager.Location, damager.Map);
            }
        }

        _dungeonPoints.Remove(damager);
    }

    public static void Serialize(IGenericWriter writer)
    {
        writer.Write(0);

        writer.Write(Enabled);

        if (TOSDSpawner.Instance != null)
        {
            writer.Write(0);
            TOSDSpawner.Instance.Serialize(writer);
        }
        else
        {
            writer.Write(1);
        }

        writer.Write(_dungeonPoints.Count);

        foreach (var (mobile, points) in _dungeonPoints)
        {
            writer.Write(mobile);
            writer.Write(points);
        }
    }

    public static void Deserialize(IGenericReader reader)
    {
        reader.ReadInt();

        Enabled = reader.ReadBool();

        if (reader.ReadInt() == 0)
        {
            var spawner = new TOSDSpawner(false);
            spawner.Deserialize(reader);
            spawner.BeginTimer();
        }

        var count = reader.ReadInt();

        for (var i = 0; i < count; i++)
        {
            var m = reader.ReadEntity<Mobile>();
            var points = reader.ReadInt();

            if (m != null && points > 0)
            {
                _dungeonPoints[m] = points;
            }
        }
    }
}

public class SorcerersDungeonPersistence : GenericPersistence
{
    public SorcerersDungeonPersistence() : base("TreasuresOfSorcerersDungeon", 10)
    {
    }

    public override void Serialize(IGenericWriter writer) => SorcerersDungeonData.Serialize(writer);

    public override void Deserialize(IGenericReader reader) => SorcerersDungeonData.Deserialize(reader);
}
