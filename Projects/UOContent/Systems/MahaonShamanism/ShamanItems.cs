using System;
using ModernUO.Serialization;
using Server.Gumps;
using Server.Spells.Shamanism;
using Server.Systems.MahaonShamanism;

namespace Server.Items;

/// <summary>
///     Книга шамана: четыре раздела по восемь обрядов. Обряды не «вписываются» в неё, как
///     заклинания в книгу мага, — доступ решают навык Шаманства и специализация раздела,
///     а книга только открывает гамп (ShamanBookGump). Поэтому это обычный предмет, а не
///     Spellbook: механика вписанных свитков тут ничего бы не дала.
///
///     Благословлена: это личный гримуар, терять его со смертью шаману незачем — тем более
///     что посмертные обряды через книгу всё равно не колдуются (см. SoulSeal).
/// </summary>
[SerializationGenerator(0, false)]
public partial class ShamanBook : Item
{
    [Constructible]
    // Не 0x2253: этой же графикой нарисована книга некроманта, а продаются они у одного
    // мистика — в списке товаров их было не отличить, кроме как по оттенку.
    public ShamanBook() : base(0x0FBD)
    {
        Hue = 0x48D;
        Weight = 3.0;
        LootType = LootType.Blessed;
    }

    public override string DefaultName => "книга шамана";

    public override void OnDoubleClick(Mobile from)
    {
        if (!from.Alive)
        {
            from.SendMessage(0x22, "Мёртвым книгу не листают. Если ты запечатал душу — обряды придут сами.");
            return;
        }

        if (!IsChildOf(from.Backpack) && Parent != from)
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            return;
        }

        from.CloseGump<ShamanBookGump>();
        from.SendGump(new ShamanBookGump(from, ShamanBookGump.LastPage(from)));
    }
}

/// <summary>
///     Ритуальный круг древнего обряда. Чертится из книги (ShamanBookGump) за одну кость и
///     держится десять минут — ровно на одну схватку. Временный: после перезапуска сервера
///     не восстанавливается.
/// </summary>
[SerializationGenerator(0, false)]
public partial class ShamanRitualCircle : BaseAddon
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);

    private Timer _decay;

    public ShamanRitualCircle()
    {
        AddComponent(new AddonComponent(0xFE7), -1, -1, 0);
        AddComponent(new AddonComponent(0xFE8), 0, -1, 0);
        AddComponent(new AddonComponent(0xFEB), 1, -1, 0);
        AddComponent(new AddonComponent(0xFE6), -1, 0, 0);
        AddComponent(new AddonComponent(0xFEA), 0, 0, 0);
        AddComponent(new AddonComponent(0xFEE), 1, 0, 0);
        AddComponent(new AddonComponent(0xFE9), -1, 1, 0);
        AddComponent(new AddonComponent(0xFEC), 0, 1, 0);
        AddComponent(new AddonComponent(0xFED), 1, 1, 0);

        Hue = 0x497;
        _decay = Timer.DelayCall(Lifetime, Delete);
    }

    public override BaseAddonDeed Deed => null;

    /// <summary>Круг, в котором стоит шаман (центр — в пределах клетки).</summary>
    public static ShamanRitualCircle FindAt(Mobile m)
    {
        if (m.Map == null || m.Map == Map.Internal)
        {
            return null;
        }

        foreach (var item in m.Map.GetItemsInRange(m.Location, 1))
        {
            if (item is ShamanRitualCircle circle && Math.Abs(item.Z - m.Z) <= 8)
            {
                return circle;
            }
        }

        return null;
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();
        _decay?.Stop();
    }

    [AfterDeserialization]
    private void AfterDeserialization() => Timer.DelayCall(Delete);
}

/// <summary>
///     Редкий компонент вершин раздела. Один на традицию: седьмой обряд берёт одну штуку,
///     восьмой — две. Падает с сильных существ (ShamanComponentDrops).
/// </summary>
[SerializationGenerator(0, false)]
public abstract partial class ShamanComponent : Item
{
    protected ShamanComponent(int itemId, int hue, int amount) : base(itemId)
    {
        Hue = hue;
        Stackable = true;
        Amount = amount;
        Weight = 0.5;
    }

    public abstract ShamanTradition Tradition { get; }

    public static Type TypeFor(ShamanTradition tradition) => tradition switch
    {
        ShamanTradition.Orc     => typeof(VentCinder),
        ShamanTradition.Goblin  => typeof(RottenHeart),
        ShamanTradition.Ogre    => typeof(MountainHeart),
        ShamanTradition.Ancient => typeof(ThunderbirdFeather),
        _                       => null
    };

    public static string NameFor(ShamanTradition tradition) => tradition switch
    {
        ShamanTradition.Orc     => "уголь из жерла",
        ShamanTradition.Goblin  => "гнилое сердце",
        ShamanTradition.Ogre    => "сердце горы",
        ShamanTradition.Ancient => "перо грозовой птицы",
        _                       => "?"
    };

    public static Item Create(ShamanTradition tradition, int amount = 1) =>
        tradition switch
        {
            ShamanTradition.Orc     => new VentCinder(amount),
            ShamanTradition.Goblin  => new RottenHeart(amount),
            ShamanTradition.Ogre    => new MountainHeart(amount),
            ShamanTradition.Ancient => new ThunderbirdFeather(amount),
            _                       => null
        };
}

[SerializationGenerator(0, false)]
public partial class VentCinder : ShamanComponent
{
    [Constructible]
    public VentCinder(int amount = 1) : base(0x19B7, 0x489, amount)
    {
    }

    public override string DefaultName => "уголь из жерла";
    public override ShamanTradition Tradition => ShamanTradition.Orc;
}

[SerializationGenerator(0, false)]
public partial class RottenHeart : ShamanComponent
{
    [Constructible]
    public RottenHeart(int amount = 1) : base(0x1CED, 0x48F, amount)
    {
    }

    public override string DefaultName => "гнилое сердце";
    public override ShamanTradition Tradition => ShamanTradition.Goblin;
}

[SerializationGenerator(0, false)]
public partial class MountainHeart : ShamanComponent
{
    [Constructible]
    public MountainHeart(int amount = 1) : base(0x1363, 0x455, amount)
    {
    }

    public override string DefaultName => "сердце горы";
    public override ShamanTradition Tradition => ShamanTradition.Ogre;
}

[SerializationGenerator(0, false)]
public partial class ThunderbirdFeather : ShamanComponent
{
    [Constructible]
    public ThunderbirdFeather(int amount = 1) : base(0x1BD1, 0x4F2, amount)
    {
    }

    public override string DefaultName => "перо грозовой птицы";
    public override ShamanTradition Tradition => ShamanTradition.Ancient;
}
