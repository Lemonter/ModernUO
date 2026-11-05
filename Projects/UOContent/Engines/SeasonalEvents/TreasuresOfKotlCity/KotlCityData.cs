using System;
using System.Collections.Generic;
using ModernUO.CodeGeneratedEvents;
using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Engines.SeasonalEvents;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfKotlCity/KotlCityData.cs).
///     ServUO's <c>PointsSystem</c> base does not exist here, so this is a plain static store with its own
///     <see cref="Enabled" /> switch (default false). Dropped: the <c>KotlBattleSimulator</c> x4 fame bonus (the
///     simulator is not ported) and the reforging drop (<c>RunicReforging</c> does not exist — a plain random magic
///     item is dropped instead, the same substitution the Doom port uses).
/// </summary>
public static class KotlCityData
{
    private static readonly Dictionary<Mobile, int> _dungeonPoints = new();

    private static KotlCityPersistence _persistence;

    /// <summary>Сезонный переключатель Treasures of Kotl City. По умолчанию выключен.</summary>
    public static bool Enabled { get; set; }

    public static bool InSeason => Enabled;

    public static void Configure()
    {
        _persistence = new KotlCityPersistence();
    }

    [OnEvent(nameof(PlayerMobile.PlayerDeletedEvent))]
    public static void OnPlayerDeleted(Mobile m) => _dungeonPoints.Remove(m);

    [OnEvent(nameof(CreatureEvents.CreatureDeathEvent))]
    public static void OnCreatureDeath(BaseCreature bc)
    {
        if (bc is SpectralKotlWarrior or SpectralMyrmidexWarrior)
        {
            AwardKill(bc, bc.LastKiller);
        }
    }

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

    /// <summary>ServUO's <c>PointsSystem.TreasuresOfKotlCity.ProcessKill</c>.</summary>
    public static void AwardKill(BaseCreature victim, Mobile damager)
    {
        if (!InSeason || victim == null || damager is not PlayerMobile pm || !damager.Alive || damager.Deleted)
        {
            return;
        }

        if (victim.Controlled || victim.Summoned || victim.Region?.IsPartOf("KotlCity") != true)
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
        damager.SendLocalizedMessage(1062317); // For your valor in combating the fallen beast, a special artifact has been bestowed on you.

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

public class KotlCityPersistence : GenericPersistence
{
    public KotlCityPersistence() : base("TreasuresOfKotlCity", 10)
    {
    }

    public override void Serialize(IGenericWriter writer) => KotlCityData.Serialize(writer);

    public override void Deserialize(IGenericReader reader) => KotlCityData.Deserialize(reader);
}
