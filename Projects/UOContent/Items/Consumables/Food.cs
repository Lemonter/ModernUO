using System;
using ModernUO.Serialization;
using Server.Engines.Craft;

namespace Server.Items;

/// <summary>
///     Портировано из ServUO. ItemQuality и IQuality отсутствуют, качество хранится как
///     ArmorQuality, OnCraft принимает BaseTool.
/// </summary>
[SerializationGenerator(0, false)]
public partial class ThreeTieredCake : Item, ICraftable
{
    [SerializableProperty(0)]
    [CommandProperty(AccessLevel.GameMaster)]
    public ArmorQuality Quality
    {
        get => _quality;
        set
        {
            _quality = value;
            this.MarkDirty();
            InvalidateProperties();
        }
    }

    [SerializableProperty(1)]
    [CommandProperty(AccessLevel.GameMaster)]
    public int Pieces
    {
        get => _pieces;
        set
        {
            _pieces = value;
            this.MarkDirty();

            if (_pieces <= 0)
            {
                Delete();
            }
        }
    }

    [Constructible]
    public ThreeTieredCake() : base(0x4BA3)
    {
        Weight = 1.0;
        Pieces = 10;
    }

    public override int LabelNumber => 1098235; // A Three Tiered Cake

    public int OnCraft(
        int quality, bool makersMark, Mobile from, CraftSystem craftSystem, Type typeRes, BaseTool tool,
        CraftItem craftItem, int resHue
    )
    {
        Quality = (ArmorQuality)quality;

        return quality;
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            return;
        }

        var cake = new Cake
        {
            ItemID = 0x4BA4
        };

        // *You cut a slice from the cake.*
        from.PrivateOverheadMessage(MessageType.Regular, 1154, 1157341, from.NetState);
        from.AddToBackpack(cake);

        Pieces--;
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (_quality == ArmorQuality.Exceptional)
        {
            list.Add(1060636); // Exceptional
        }
    }
}

[SerializationGenerator(0, false)]
public partial class Hamburger : Food
{
    [Constructible]
    public Hamburger(int amount = 1) : base(0xA0DA, amount) => FillFactor = 2;

    public override int LabelNumber => 1125202; // hamburger
}

[Flippable(0xA0D8, 0xA0D9)]
[SerializationGenerator(0, false)]
public partial class HotDog : Food
{
    [Constructible]
    public HotDog(int amount = 1) : base(0xA0D8, amount) => FillFactor = 2;

    public override int LabelNumber => 1125201; // hot dog
}

[Flippable(0xA0D6, 0xA0D7)]
[SerializationGenerator(0, false)]
public partial class CookableSausage : Food
{
    [Constructible]
    public CookableSausage() : base(0xA0D6) => FillFactor = 2;

    public override int LabelNumber => 1125198; // sausage
}

[SerializationGenerator(0, false)]
public partial class PulledPorkPlatter : Food
{
    [Constructible]
    public PulledPorkPlatter() : base(0x999F)
    {
        FillFactor = 5;
        Stackable = false;
        Hue = 1157;
    }

    public override int LabelNumber => 1123351; // Pulled Pork Platter
}

[SerializationGenerator(0, false)]
public partial class PulledPorkSandwich : Food
{
    [Constructible]
    public PulledPorkSandwich() : base(0x99A0)
    {
        FillFactor = 3;
        Stackable = false;
    }

    public override int LabelNumber => 1123352; // Pulled Pork Sandwich
}
