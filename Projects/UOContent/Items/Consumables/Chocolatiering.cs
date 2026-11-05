using System;
using ModernUO.Serialization;
using Server.Engines.Craft;

namespace Server.Items;

/// <summary>
///     Портировано из ServUO. Остальные типы из этого файла в репозитории уже есть. ItemQuality и
///     IQuality отсутствуют: качество хранится как ArmorQuality, OnCraft принимает BaseTool.
/// </summary>
[SerializationGenerator(0, false)]
public partial class SweetCocoaButter : Item, ICraftable
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

    [Constructible]
    public SweetCocoaButter() : base(0x103D)
    {
    }

    public override int LabelNumber => 1156401; // Sweet Cocoa butter

    public override double DefaultWeight => 1.0;

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (_quality == ArmorQuality.Exceptional)
        {
            list.Add(1060636); // Exceptional
        }
    }

    public int OnCraft(
        int quality, bool makersMark, Mobile from, CraftSystem craftSystem, Type typeRes, BaseTool tool,
        CraftItem craftItem, int resHue
    )
    {
        Quality = (ArmorQuality)quality;

        return quality;
    }
}
