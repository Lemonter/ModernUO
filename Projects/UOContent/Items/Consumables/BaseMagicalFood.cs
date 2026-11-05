using System;
using System.Collections.Generic;
using ModernUO.Serialization;

namespace Server.Items;

public enum MagicalFood
{
    None = 0x0,
    GrapesOfWrath = 0x1,
    EnchantedApple = 0x2
}

/// <summary>
///     Портировано из ServUO. Влияние и перезарядка хранятся в статических словарях и не
///     переживают перезапуск сервера.
/// </summary>
[SerializationGenerator(0, false)]
public partial class BaseMagicalFood : Food
{
    private static readonly Dictionary<Mobile, int> _table = new();
    private static readonly Dictionary<Mobile, int> _cooldown = new();

    [Constructible]
    public BaseMagicalFood(int itemID) : base(itemID)
    {
        Weight = 1.0;
        FillFactor = 0;
        Stackable = false;
    }

    public virtual MagicalFood FoodID => MagicalFood.None;

    public virtual TimeSpan Cooldown => TimeSpan.Zero;

    public virtual TimeSpan Duration => TimeSpan.Zero;

    public virtual int EatMessage => 0;

    public static bool IsUnderInfluence(Mobile mob, MagicalFood id) =>
        _table.TryGetValue(mob, out var value) && (value & (int)id) > 0;

    public static bool CoolingDown(Mobile mob, MagicalFood id) =>
        _cooldown.TryGetValue(mob, out var value) && (value & (int)id) > 0;

    public static void StartInfluence(Mobile mob, MagicalFood id, TimeSpan duration, TimeSpan cooldown)
    {
        _table.TryGetValue(mob, out var value);
        _table[mob] = value | (int)id;

        Timer.DelayCall(duration, () => EndInfluence(mob, id, cooldown));
    }

    public static void EndInfluence(Mobile mob, MagicalFood id, TimeSpan cooldown)
    {
        _table.TryGetValue(mob, out var value);
        _table[mob] = value & ~(int)id;

        if (cooldown == TimeSpan.Zero)
        {
            return;
        }

        _cooldown.TryGetValue(mob, out var cooldownValue);
        _cooldown[mob] = cooldownValue | (int)id;

        Timer.DelayCall(cooldown, () => EndCooldown(mob, id));
    }

    public static void EndCooldown(Mobile mob, MagicalFood id)
    {
        _cooldown.TryGetValue(mob, out var value);
        _cooldown[mob] = value & ~(int)id;
    }

    public override bool Eat(Mobile from)
    {
        if (IsUnderInfluence(from, FoodID))
        {
            return false;
        }

        if (CoolingDown(from, FoodID))
        {
            from.SendLocalizedMessage(1070772); // You must wait a few seconds before you can use that item.
            return false;
        }

        from.SendLocalizedMessage(EatMessage);

        StartInfluence(from, FoodID, Duration, Cooldown);
        Consume();

        return true;
    }
}
