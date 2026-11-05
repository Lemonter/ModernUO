using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Портировано из ServUO. В здешнем BeverageType нет значений HotCocoa/Coffee/GreenTea, поэтому
///     кружки сделаны обычными предметами с той же графикой и без механики наполнения.
/// </summary>
[SerializationGenerator(0, false)]
public partial class HotCocoaMug : Item
{
    [Constructible]
    public HotCocoaMug() : base(0x995) => Name = "кружка горячего шоколада";

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            return;
        }

        from.PublicOverheadMessage(MessageType.Regular, 0x3B2, 1155739); // *You sip from the mug*
    }
}

/// <summary>
///     Портировано из ServUO. В здешнем BeverageType нет значений HotCocoa/Coffee/GreenTea, поэтому
///     кружки сделаны обычными предметами с той же графикой и без механики наполнения.
/// </summary>
[SerializationGenerator(0, false)]
public partial class BasketOfGreenTeaMug : Item
{
    [Constructible]
    public BasketOfGreenTeaMug() : base(0x995) => Name = "кружка зелёного чая";

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            return;
        }

        from.PublicOverheadMessage(MessageType.Regular, 0x3B2, 1155739); // *You sip from the mug*
    }
}

/// <summary>
///     Портировано из ServUO. В здешнем BeverageType нет значений HotCocoa/Coffee/GreenTea, поэтому
///     кружки сделаны обычными предметами с той же графикой и без механики наполнения.
/// </summary>
[SerializationGenerator(0, false)]
public partial class CoffeeMug : Item
{
    [Constructible]
    public CoffeeMug() : base(0x995) => Name = "кружка кофе";

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            return;
        }

        from.PublicOverheadMessage(MessageType.Regular, 0x3B2, 1155739); // *You sip from the mug*
    }
}
