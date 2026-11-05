using ModernUO.Serialization;

namespace Server.Items;

[Flippable]
[SerializationGenerator(0, false)]
public partial class BaseHolidayWallArt : BaseLight
{
    private static readonly string[] _names =
    {
        "Минока", "Британии", "Хартвуда", "Аббатства Эмпат", "Лицея", "Нового Хейвена", "Новой Маджинции", "Иодона", "Луны",
        "Делюсии", "Буканир Дена", "Тринсика", "Винда", "Джелома", "Зенто", "Нужельма", "Папуа", "Мунглоу", "Минока",
        "Скара Брая"
    };

    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private string _displayName;

    [Constructible]
    public BaseHolidayWallArt(int itemID) : base(itemID)
    {
        _displayName = _names[Utility.Random(_names.Length)];
        Burning = false;
        Light = LightType.Circle225;
        Weight = 1.0;
    }

    public override int LabelNumber => 1126181; // glass tree

    public override int LitItemID => 0;

    public override int UnlitItemID => 0;

    public virtual int NorthID => 0;

    public virtual int WestID => 0;

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (!string.IsNullOrEmpty(_displayName))
        {
            list.Add(1159260, _displayName); // Crafted By A Glassblower From ~1_WHERE~
        }
    }

    public void Flip()
    {
        if (ItemID == NorthID)
        {
            ItemID = WestID;
        }
        else if (ItemID == WestID)
        {
            ItemID = NorthID;
        }
    }
}

[SerializationGenerator(0, false)]
public partial class HolidayWallArt1 : BaseHolidayWallArt
{
    [Constructible]
    public HolidayWallArt1() : base(0xA4B3)
    {
    }

    public override int LitItemID => ItemID == 0xA4B3 ? 0xA4B4 : 0xA4B6;

    public override int UnlitItemID => ItemID == 0xA4B4 ? 0xA4B3 : 0xA4B5;

    public override int NorthID => Burning ? 0xA4B4 : 0xA4B3;

    public override int WestID => Burning ? 0xA4B6 : 0xA4B5;
}

[SerializationGenerator(0, false)]
public partial class HolidayWallArt2 : BaseHolidayWallArt
{
    [Constructible]
    public HolidayWallArt2() : base(0xA4B7)
    {
    }

    public override int LitItemID => ItemID == 0xA4B7 ? 0xA4B8 : 0xA4BA;

    public override int UnlitItemID => ItemID == 0xA4B8 ? 0xA4B7 : 0xA4B9;

    public override int NorthID => Burning ? 0xA4B8 : 0xA4B7;

    public override int WestID => Burning ? 0xA4BA : 0xA4B9;
}

[SerializationGenerator(0, false)]
public partial class HolidayWallArt3 : BaseHolidayWallArt
{
    [Constructible]
    public HolidayWallArt3() : base(0xA4AD)
    {
    }

    public override int LitItemID => ItemID == 0xA4AD ? 0xA4AE : 0xA4B0;

    public override int UnlitItemID => ItemID == 0xA4AE ? 0xA4AD : 0xA4AF;

    public override int NorthID => Burning ? 0xA4AE : 0xA4AD;

    public override int WestID => Burning ? 0xA4B0 : 0xA4AF;
}
