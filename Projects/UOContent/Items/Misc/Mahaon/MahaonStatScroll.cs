using ModernUO.Serialization;
using Server.Mobiles;
using Server.Systems.MahaonProfessions;

namespace Server.Items;

/// <summary>
///     Свиток на характеристики, который на нашем шарде действительно работает.
///
///     Ванильный StatCapScroll задаёт предел суммы АБСОЛЮТНЫМ числом: «свиток +5» — это на
///     самом деле «поставить предел 230», и он отказывается работать, если у тебя уже
///     больше (CanUse: «Your stats are too high for this power scroll»). На чистой ультиме
///     это верно, потому что там у всех ровно 225.
///
///     У нас профессия выставляет предел суммы сама — 300 у всех, — и он заведомо выше
///     ванильных 230-250. Значит, каждый ванильный свиток из лавки Гвидо отказывался
///     работать, и игрок платил тысячи золота за бумагу, которая по-английски сообщала
///     ему, что он слишком хорош.
///
///     Этот свиток ПРИБАВЛЯЕТ к пределу суммы, поэтому работает всегда. Потолок прибавок
///     доводит сумму до 375 — это ровно три общешардовых предела на характеристику
///     (stats.statMax = 125), то есть полный набор свитков позволяет выкачать все три
///     характеристики до упора, и ни на единицу больше.
/// </summary>
[SerializationGenerator(0, false)]
public partial class MahaonStatScroll : Item
{
    [SerializableField(0)]
    private int _bonus;

    [Constructible]
    public MahaonStatScroll(int bonus = 5) : base(0x14F0)
    {
        _bonus = bonus;
        Hue = 0x481;
        Weight = 1.0;
        LootType = LootType.Regular;
    }

    public override string DefaultName => $"свиток характеристик (+{_bonus})";

    /// <summary>До какой суммы свитки доводят предел. 375 = 3 × stats.statMax.</summary>
    public const int MaxStatCap = 375;

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add($"Поднимает предел суммы характеристик на {_bonus} (не выше {MaxStatCap})");
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendMessage(0x22, "Свиток должен быть у тебя в рюкзаке.");
            return;
        }

        if (from.StatCap >= MaxStatCap)
        {
            from.SendMessage(0x22, $"Предел суммы характеристик уже на потолке — {MaxStatCap}. Выше свитки не уводят.");
            return;
        }

        var before = from.StatCap;

        from.StatCap = System.Math.Min(from.StatCap + _bonus, MaxStatCap);

        from.SendMessage(
            0x59,
            $"Предел суммы характеристик поднят с {before} до {from.StatCap} (потолок — {MaxStatCap})."
        );

        from.PlaySound(0x1F7);
        from.FixedParticles(0x373A, 10, 15, 5018, EffectLayer.Waist);

        Delete();
    }
}
