using ModernUO.Serialization;
using Server.Gumps;

namespace Server.Items;

public enum HolidayGift2018
{
    None = 0,
    Wreath = 1,
    Sign = 2,
    RecipeBook = 3
}

/// <summary>
///     Портировано из ServUO. Класса RecipeBook в этом репозитории нет, поэтому вариант
///     «книга рецептов» выдаёт только коробку с обычными сладостями.
/// </summary>
[SerializationGenerator(0, false)]
public partial class HolidayGiftToken2018 : Item, IRewardOption
{
    [SerializableField(0)]
    private HolidayGift2018 _gift;

    [Constructible]
    public HolidayGiftToken2018() : this(HolidayGift2018.None)
    {
    }

    [Constructible]
    public HolidayGiftToken2018(HolidayGift2018 gift) : base(0xA094)
    {
        LootType = LootType.Blessed;
        Light = LightType.Circle300;
        Weight = 1.0;
        _gift = gift;
        Hue = 1371;
    }

    public override int LabelNumber => 1158831; // 2018 Holiday Gift Token

    public void GetOptions(RewardOptionList list)
    {
        list.Add((int)HolidayGift2018.Wreath, 1158812);
        list.Add((int)HolidayGift2018.Sign, 1158811);
        list.Add((int)HolidayGift2018.RecipeBook, 1158810);
    }

    public void OnOptionSelected(Mobile from, int choice)
    {
        _gift = (HolidayGift2018)choice;

        if (!Deleted)
        {
            GiveGift(from);
        }
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (IsChildOf(from.Backpack))
        {
            RewardOptionGump.DisplayTo(from, this, 1156381);
        }
        else
        {
            from.SendLocalizedMessage(1062334); // This item must be in your backpack to be used.
        }
    }

    public void GiveGift(Mobile from)
    {
        var box = new GiftBox();

        var gift = new GingerBreadCookie
        {
            LootType = LootType.Regular
        };

        box.DropItem(gift);

        box.DropItem(Utility.Random(100) < 60 ? new Poinsettia(33) : new Poinsettia(1154));

        switch (_gift)
        {
            case HolidayGift2018.Sign:
                {
                    box.DropItem(new HolidaysSign());
                    break;
                }
            case HolidayGift2018.Wreath:
                {
                    box.DropItem(new HolidayWreath());
                    break;
                }
        }

        from.AddToBackpack(box);

        Delete();
    }
}
