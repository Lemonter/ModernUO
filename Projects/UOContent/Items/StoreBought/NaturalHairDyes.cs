using ModernUO.Serialization;
using Server.Gumps;
using Server.Network;

namespace Server.Items;

public enum HairDyeType
{
    None,
    LemonLime,
    YewBrown,
    BloodwoodRed,
    VividBlue,
    AshBlonde,
    HeartwoodGreen,
    OakBlonde,
    SacredWhite,
    FrostwoodIceGreen,
    FieryBlonde,
    BitterBrown,
    GnawsTwistedBlue,
    DuskBlack,
}

[SerializationGenerator(0, false)]
public partial class NaturalHairDye : Item
{
    public static HairDyeInfo[] Table { get; } =
    [
        new(HairDyeType.None, 0, -1),
        new(HairDyeType.LemonLime, 1167, 1071439),
        new(HairDyeType.YewBrown, 1192, 1071470),
        new(HairDyeType.BloodwoodRed, 1194, 1071471),
        new(HairDyeType.VividBlue, 1152, 1071438),
        new(HairDyeType.AshBlonde, 1191, 1071469),
        new(HairDyeType.HeartwoodGreen, 1193, 1071472),
        new(HairDyeType.OakBlonde, 2010, 1071468),
        new(HairDyeType.SacredWhite, 1153, 1071474),
        new(HairDyeType.FrostwoodIceGreen, 1151, 1071473),
        new(HairDyeType.FieryBlonde, 1174, 1071440),
        new(HairDyeType.BitterBrown, 1149, 1071437),
        new(HairDyeType.GnawsTwistedBlue, 1195, 1071442),
        new(HairDyeType.DuskBlack, 1175, 1071441),
    ];

    [SerializableField(0, fieldChanged: nameof(OnTypeChanged))]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private HairDyeType _type;

    [Constructible]
    public NaturalHairDye(HairDyeType type = HairDyeType.None) : base(0xEFE)
    {
        Weight = 1.0;
        _type = type;
        ApplyHue();
    }

    public override int LabelNumber => 1071387; // Natural Hair Dye

    public static HairDyeInfo GetInfo(HairDyeType type)
    {
        foreach (var info in Table)
        {
            if (info.Type == type)
            {
                return info;
            }
        }

        return null;
    }

    private void OnTypeChanged(HairDyeType oldValue, HairDyeType newValue) => ApplyHue();

    private void ApplyHue()
    {
        var info = GetInfo(_type);
        Hue = info?.Hue ?? 0;
        InvalidateProperties();
    }

    [AfterDeserialization]
    private void AfterDeserialization() => ApplyHue();

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        var info = GetInfo(_type);

        if (info?.Localization > 0)
        {
            list.Add(info.Localization);
        }
    }

    public override void OnDoubleClick(Mobile m)
    {
        if (!IsChildOf(m.Backpack))
        {
            m.SendLocalizedMessage(1042010); // You must have the object in your backpack to use it.
            return;
        }

        m.SendGump(new HairDyeConfirmGump(this, Hue));
    }
}

public class HairDyeInfo
{
    public HairDyeInfo(HairDyeType type, int hue, int localization)
    {
        Type = type;
        Hue = hue;
        Localization = localization;
    }

    public HairDyeType Type { get; }
    public int Hue { get; }
    public int Localization { get; }
}

public class HairDyeConfirmGump : StaticGump<HairDyeConfirmGump>
{
    private readonly Item _dye;
    private readonly int _hue;

    public override bool Singleton => true;

    public HairDyeConfirmGump(Item dye, int hue) : base(200, 200)
    {
        _dye = dye;
        _hue = hue;
    }

    protected override void BuildLayout(ref StaticGumpBuilder builder)
    {
        builder.AddPage();

        builder.AddBackground(0, 0, 340, 200, 3600);
        builder.AddBackground(0, 0, 100, 100, 3600);
        builder.AddBackground(100, 140, 227, 50, 9270);

        builder.AddImage(-39, -23, 50702, _hue - 1);

        builder.AddHtmlLocalized(110, 25, 205, 80, 1074396, 0x0080FF); // This special hair dye is made of a unique mixture of leaves...
        builder.AddHtmlLocalized(120, 155, 160, 20, 1074395, 0x00FFFF); // <div align=right>Use Permanent Hair Dye</div>

        builder.AddButton(290, 157, 0x7538, 0x7539, 1);
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (info.ButtonID != 1)
        {
            return;
        }

        var m = sender.Mobile;

        if (_dye.Deleted || !_dye.IsChildOf(m.Backpack))
        {
            m.SendLocalizedMessage(1042010); // You must have the object in your backpack to use it.
            return;
        }

        if (m.HairItemID == 0)
        {
            m.SendLocalizedMessage(502623); // You have no hair to dye and you cannot use this.
            return;
        }

        m.HairHue = _hue;
        m.SendLocalizedMessage(501199); // You dye your hair
        _dye.Delete();
        m.PlaySound(0x4E);
    }
}
