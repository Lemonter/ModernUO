using System;
using System.Collections.Generic;
using ModernUO.CodeGeneratedEvents;
using ModernUO.Serialization;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.SeasonalEvents;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfKhaldun/KhaldunData.cs).
///     ServUO's <c>PointsSystem</c> base class does not exist here, so this is a plain static store: the seasonal
///     switch is <see cref="Enabled" /> (default false) and kills reach it through the generated creature-death
///     event (the Khaldun roster only). ServUO's reforging reward
///     (<c>RunicReforging.GenerateRandomItem(..., ReforgedSuffix.Khaldun)</c>) is replaced with a plain random magic
///     item: <c>RunicReforging</c> does not exist in this codebase.
/// </summary>
public static class KhaldunData
{
    private static readonly Dictionary<Mobile, int> _dungeonPoints = new();

    private static KhaldunPersistence _persistence;

    /// <summary>Сезонный переключатель Treasures of Khaldun. По умолчанию выключен.</summary>
    public static bool Enabled { get; set; }

    /// <summary>Признак того, что сюжетный контент (квест «Going Gumshoe») уже размещён на карте.</summary>
    public static bool QuestContentGenerated { get; set; }

    public static bool InSeason => Enabled;

    public static void Configure()
    {
        _persistence = new KhaldunPersistence();
    }

    [OnEvent(nameof(PlayerMobile.PlayerDeletedEvent))]
    public static void OnPlayerDeleted(Mobile m) => _dungeonPoints.Remove(m);

    /// <summary>
    ///     ServUO let <c>PointsSystem.ProcessKill</c> see every kill; the closest hook here is the generated
    ///     creature-death event, filtered to the seasonal Khaldun roster (ServUO's <c>IsChampionSpawn</c> flag does
    ///     not exist).
    /// </summary>
    [OnEvent(nameof(CreatureEvents.CreatureDeathEvent))]
    public static void OnCreatureDeath(BaseCreature bc)
    {
        if (bc is CultistAmbusher or KhalAnkurWarriors or KhaldunBlood or SkelementalKnight or SkelementalMage or KhalAnkur)
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

    /// <summary>
    ///     ServUO's <c>PointsSystem.Khaldun.ProcessKill</c>: creatures slain inside the Khaldun region award dungeon
    ///     points scaled by fame and luck, and a lucky roll drops a magic item. ServUO also required
    ///     <c>bc.IsChampionSpawn</c>; that flag does not exist here, so every eligible creature counts.
    /// </summary>
    public static void AwardKill(BaseCreature victim, Mobile damager)
    {
        if (!InSeason || victim == null || damager is not PlayerMobile pm || !damager.Alive || damager.Deleted)
        {
            return;
        }

        if (victim.Controlled || victim.Summoned || victim.Region?.IsPartOf("Khaldun") != true)
        {
            return;
        }

        var fame = victim.Fame / 4;
        var luck = Math.Max(0, pm.Luck);

        AwardPoints(pm, (int)(fame * (1 + Math.Sqrt(luck) / 100)) * PotionOfGloriousFortune.GetBonus(damager));

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

        KhaldunTastyTreat.SerializeTable(writer);
        PotionOfGloriousFortune.SerializeTable(writer);

        writer.Write(Enabled);
        writer.Write(QuestContentGenerated);

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

        KhaldunTastyTreat.DeserializeTable(reader);
        PotionOfGloriousFortune.DeserializeTable(reader);

        Enabled = reader.ReadBool();
        QuestContentGenerated = reader.ReadBool();

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

public class KhaldunPersistence : GenericPersistence
{
    public KhaldunPersistence() : base("TreasuresOfKhaldun", 10)
    {
    }

    public override void Serialize(IGenericWriter writer) => KhaldunData.Serialize(writer);

    public override void Deserialize(IGenericReader reader) => KhaldunData.Deserialize(reader);
}
