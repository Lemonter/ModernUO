using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Engines.BuffIcons;
using Server.Mobiles;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfKhaldun/Items/PotionOfGloriousFortune.cs).
///     ServUO's <c>BuffInfo.AddBuff(m, ...)</c> becomes <c>(m as PlayerMobile)?.AddBuff(...)</c> here.
/// </summary>
[SerializationGenerator(0, false)]
public partial class PotionOfGloriousFortune : Item
{
    public static int Bonus = 400;

    public static Dictionary<Mobile, DateTime> Table { get; set; }

    private static TimerExecutionToken _tickToken;

    public override int LabelNumber => 1158688; // Potion of Glorious Fortune

    [Constructible]
    public PotionOfGloriousFortune() : base(0xA1E6)
    {
        Hue = 1195;
        LootType = LootType.Blessed;
    }

    public static int GetBonus(Mobile m) => UnderEffects(m) ? Bonus / 100 : 1;

    public static bool TryAddEffects(Mobile m)
    {
        if (UnderEffects(m))
        {
            m.SendLocalizedMessage(1158718); // You are already under the effect of the Potion of Glorious Fortune.
            return false;
        }

        Table ??= new Dictionary<Mobile, DateTime>();
        Table[m] = Core.Now + TimeSpan.FromHours(1);

        m.SendLocalizedMessage(1158719); // You are now under the effect of the Potion of Glorious Fortune.

        (m as PlayerMobile)?.AddBuff(
            new BuffInfo(BuffIcon.PotionGloriousFortune, 1158688, TimeSpan.FromMinutes(60), $"{Bonus}")
        );

        m.FixedEffect(0x375A, 10, 15);
        m.PlaySound(0x1E7);

        if (!_tickToken.Running)
        {
            Timer.StartTimer(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5), OnTick, out _tickToken);
        }

        return true;
    }

    public static bool UnderEffects(Mobile m)
    {
        if (Table == null || !Table.TryGetValue(m, out var expiry))
        {
            return false;
        }

        if (expiry < Core.Now)
        {
            ExpireBuff(m);
            return false;
        }

        return true;
    }

    public static void ExpireBuff(Mobile m)
    {
        Table?.Remove(m);

        if (Table is not { Count: > 0 })
        {
            Table = null;
            _tickToken.Cancel();
        }
    }

    private static void OnTick()
    {
        if (Table == null)
        {
            _tickToken.Cancel();
            return;
        }

        var expired = new List<Mobile>();

        foreach (var (mobile, _) in Table)
        {
            if (!UnderEffects(mobile))
            {
                expired.Add(mobile);
            }
        }

        foreach (var mobile in expired)
        {
            ExpireBuff(mobile);
        }
    }

    public static void SerializeTable(IGenericWriter writer)
    {
        writer.Write(0);
        writer.Write(Table?.Count ?? 0);

        if (Table == null)
        {
            return;
        }

        foreach (var (mobile, expiry) in Table)
        {
            writer.Write(mobile);
            writer.Write(expiry);
        }
    }

    public static void DeserializeTable(IGenericReader reader)
    {
        reader.ReadInt();

        var count = reader.ReadInt();

        for (var i = 0; i < count; i++)
        {
            var mobile = reader.ReadEntity<Mobile>();
            var expiry = reader.ReadDateTime();

            if (mobile == null || expiry <= Core.Now)
            {
                continue;
            }

            Table ??= new Dictionary<Mobile, DateTime>();
            Table[mobile] = expiry;

            if (!_tickToken.Running)
            {
                Timer.StartTimer(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5), OnTick, out _tickToken);
            }
        }
    }

    public override void OnDoubleClick(Mobile m)
    {
        if (IsChildOf(m.Backpack) && TryAddEffects(m))
        {
            Consume();
        }
    }
}

public class PotionOfGloriousFortunePersistence : GenericPersistence
{
    public PotionOfGloriousFortunePersistence() : base("PotionOfGloriousFortune", 20)
    {
    }

    public override void Serialize(IGenericWriter writer) => PotionOfGloriousFortune.SerializeTable(writer);

    public override void Deserialize(IGenericReader reader) => PotionOfGloriousFortune.DeserializeTable(reader);
}
