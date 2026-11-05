using System;
using System.Collections.Generic;
using ModernUO.CodeGeneratedEvents;
using ModernUO.Serialization;
using Server.Engines.VvV;
using Server.Mobiles;

namespace Server.Engines.SeasonalEvents;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfDoom/DoomData.cs).
///     ServUO's <c>PointsSystem</c> base class and its automatic kill hook do not exist here, so this is a plain
///     static store: the seasonal switch is <see cref="Enabled" /> (default false) and kills must be reported by
///     the caller through <see cref="AwardKill" />. ServUO's reforging reward
///     (<c>RunicReforging.GenerateRandomItem(..., ReforgedSuffix.Doom)</c>) is replaced with a plain random magic
///     item: <c>RunicReforging</c> does not exist in this codebase.
/// </summary>
public static class DoomData
{
    private static readonly Dictionary<Mobile, int> _dungeonPoints = new();

    private static DoomPersistence _persistence;

    /// <summary>Сезонный переключатель Treasures of Doom. По умолчанию выключен.</summary>
    public static bool Enabled { get; set; }

    public static bool InSeason => Enabled;

    public static void Configure()
    {
        _persistence = new DoomPersistence();
    }

    [OnEvent(nameof(PlayerMobile.PlayerDeletedEvent))]
    public static void OnPlayerDeleted(Mobile m) => _dungeonPoints.Remove(m);

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

    /// <summary>
    ///     ServUO's <c>PointsSystem.TreasuresOfDoom.ProcessKill</c>: creatures slain inside the Doom region award
    ///     dungeon points scaled by fame and luck, and a lucky roll drops a magic item.
    /// </summary>
    public static void AwardKill(BaseCreature victim, Mobile damager)
    {
        if (!InSeason || victim == null || damager is not PlayerMobile pm || !damager.Alive || damager.Deleted)
        {
            return;
        }

        // Doom lives on Malas; the region-name check ServUO used has no string overload here.
        if (victim.Controlled || victim.Summoned || victim.Map != Map.Malas)
        {
            return;
        }

        var fame = victim.Fame / 2;
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
        damager.SendLocalizedMessage(1155588); // You notice the crest of Doom on your fallen foe's equipment...

        if (!damager.PlaceInBackpack(item) && damager.BankBox?.TryDropItem(damager, item, false) != true)
        {
            damager.SendLocalizedMessage(1072523); // You find an artifact, but your backpack and bank are too full.
            item.MoveToWorld(damager.Location, damager.Map);
        }

        DeductPoints(damager, points);
    }

    public static void Serialize(IGenericWriter writer)
    {
        writer.Write(0);

        writer.Write(Enabled);
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

public class DoomPersistence : GenericPersistence
{
    public DoomPersistence() : base("TreasuresOfDoom", 10)
    {
    }

    public override void Serialize(IGenericWriter writer) => DoomData.Serialize(writer);

    public override void Deserialize(IGenericReader reader) => DoomData.Deserialize(reader);
}

