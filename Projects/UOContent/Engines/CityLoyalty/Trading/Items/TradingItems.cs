/// <summary>
///     Ported from ServUO (Scripts/Services/City Loyalty System/Trading/Items/*).
///
///     Substitutions:
///     <list type="bullet">
///         <item>
///             <c>RewardSign</c> derived from <c>Sign</c> and ServUO's <c>IEngravable</c>; that interface does not
///             exist here, so the engraving text is a plain serialized property and ServUO's <c>SignType</c> range
///             31..58 is converted straight to an item id (this codebase's <c>Sign</c> has an <c>int itemID</c>
///             constructor, so no enum member is needed).
///         </item>
///         <item><c>ObjectPropertyList</c> became <see cref="IPropertyList" />.</item>
///     </list>
/// </summary>
using ModernUO.Serialization;
using Server.Gumps;
using Server.Items;

namespace Server.Engines.CityLoyalty;

[SerializationGenerator(0, false)]
public abstract partial class CityBook : Item
{
    protected CityBook() : base(4030)
    {
    }

    public abstract int Title { get; }
    public abstract int Content { get; }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (Title > 0)
        {
            list.Add(Title);
        }

        list.Add(1154760, "Адаму Эдом"); // By: ~1_NAME~
    }

    public override void OnDoubleClick(Mobile m)
    {
        if (!IsChildOf(m.Backpack))
        {
            return;
        }

        var gump = new Gump(150, 150);
        gump.AddImage(0, 0, 30236);
        gump.AddHtmlLocalized(110, 30, 350, 630, Content, 0x7FFF, false, false);

        m.SendGump(gump);
    }

    public static CityBook GetRandom() => Utility.Random(9) switch
    {
        0 => new LoyaltyBookBritain(),
        1 => new LoyaltyBookYew(),
        2 => new LoyaltyBookMoonglow(),
        3 => new LoyaltyBookNewMagincia(),
        4 => new LoyaltyBookVesper(),
        5 => new LoyaltyBookSkaraBrae(),
        6 => new LoyaltyBookJhelom(),
        7 => new LoyaltyBookMinoc(),
        _ => new LoyaltyBookTrinsic()
    };
}

[SerializationGenerator(0, false)]
public partial class LoyaltyBookYew : CityBook
{
    [Constructible]
    public LoyaltyBookYew()
    {
    }

    public override int Title => 1153933;
    public override int Content => 1152698;
}

[SerializationGenerator(0, false)]
public partial class LoyaltyBookJhelom : CityBook
{
    [Constructible]
    public LoyaltyBookJhelom()
    {
    }

    public override int Title => 1153934;
    public override int Content => 1153436;
}

[SerializationGenerator(0, false)]
public partial class LoyaltyBookMoonglow : CityBook
{
    [Constructible]
    public LoyaltyBookMoonglow()
    {
    }

    public override int Title => 1153935;
    public override int Content => 1153437;
}

[SerializationGenerator(0, false)]
public partial class LoyaltyBookTrinsic : CityBook
{
    [Constructible]
    public LoyaltyBookTrinsic()
    {
    }

    public override int Title => 1153727;
    public override int Content => 1151755;
}

[SerializationGenerator(0, false)]
public partial class LoyaltyBookMinoc : CityBook
{
    [Constructible]
    public LoyaltyBookMinoc()
    {
    }

    public override int Title => 1153728;
    public override int Content => 1152317;
}

[SerializationGenerator(0, false)]
public partial class LoyaltyBookNewMagincia : CityBook
{
    [Constructible]
    public LoyaltyBookNewMagincia()
    {
    }

    public override int Title => 1153008;
    public override int Content => 1153723;
}

[SerializationGenerator(0, false)]
public partial class LoyaltyBookVesper : CityBook
{
    [Constructible]
    public LoyaltyBookVesper()
    {
    }

    public override int Title => 1153012;
    public override int Content => 1153724;
}

[SerializationGenerator(0, false)]
public partial class LoyaltyBookSkaraBrae : CityBook
{
    [Constructible]
    public LoyaltyBookSkaraBrae()
    {
    }

    public override int Title => 0;
    public override int Content => 1153725;
}

[SerializationGenerator(0, false)]
public partial class LoyaltyBookBritain : CityBook
{
    [Constructible]
    public LoyaltyBookBritain()
    {
    }

    public override int Title => 1153726;
    public override int Content => 1151754;
}

[SerializationGenerator(0, false)]
public partial class ExpiredVoucher : Item
{
    [Constructible]
    public ExpiredVoucher() : base(0x2831)
    {
    }

    // Expired Voucher for a Free Drink at the Fortune's Fire Casino
    public override int LabelNumber => 1151749;
}

[SerializationGenerator(0, false)]
public partial class ForgedArtwork : Item
{
    [Constructible]
    public ForgedArtwork() : base(Utility.Random(0x1224, 5)) => Weight = 10;

    // Forged Artwork from King Blackthorn's Collection
    public override int LabelNumber => 1151750;
}

[SerializationGenerator(0, false)]
public partial class LittleBlackBook : Item
{
    [Constructible]
    public LittleBlackBook() : base(4030) => Hue = 1932;

    // Slim the Fence's little black book
    public override int LabelNumber => 1151751;
}

[SerializationGenerator(0, false)]
public partial class MysteriousNote : Item
{
    [Constructible]
    public MysteriousNote() : base(0x14ED)
    {
    }

    // A Mysterious Note
    public override int LabelNumber => 1151753;

    public override void OnDoubleClick(Mobile from)
    {
        var gump = new Gump(100, 100);

        gump.AddBackground(0, 0, 404, 325, 9380);
        gump.AddHtmlLocalized(37, 50, 340, 225, 1151735, 0xC63, false, true);

        from.SendGump(gump);
    }
}

[SerializationGenerator(0, false)]
public partial class RewardSign : Sign
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private string _engravedText;

    [Constructible]
    public RewardSign() : base(0xB95 + 2 * Utility.RandomMinMax(31, 58) + Utility.Random(2)) => Movable = true;

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (!string.IsNullOrEmpty(_engravedText))
        {
            list.Add(1072305, _engravedText); // Engraved: ~1_INSCRIPTION~
        }
    }
}

[SerializationGenerator(0, false)]
public partial class SlimsShadowVeil : LeatherMempo
{
    [Constructible]
    public SlimsShadowVeil()
    {
        Hue = 1932;
        Attributes.CastSpeed = 1;
    }

    // Slim's Shadow Veil
    public override int LabelNumber => 1154906;
}
