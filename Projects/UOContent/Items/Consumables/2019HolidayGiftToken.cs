using System;
using ModernUO.Serialization;
using Server.Gumps;
using Server.Misc;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class HolidayGiftGiver2019 : GiftGiver
{
    public override DateTime Start => new(2019, 12, 03);

    public override DateTime Finish => new(2020, 01, 03);

    public override TimeSpan MinimumAge => TimeSpan.FromDays(30);

    public static void Initialize() => GiftGiving.Register(new HolidayGiftGiver2019());

    public override void GiveGift(Mobile mob)
    {
        Item token = new HolidayGiftToken2019();

        switch (GiveGift(mob, token))
        {
            case GiftResult.Backpack:
                {
                    // Happy Holidays from the Origin team! Gift items have been placed in your backpack.
                    mob.SendLocalizedMessage(1062835);
                    break;
                }
            case GiftResult.BankBox:
                {
                    // Happy Holidays from the Origin team! Gift items have been placed in your bank box.
                    mob.SendLocalizedMessage(1062836);
                    break;
                }
        }
    }
}

public enum HolidayGift2019
{
    None = 0,
    HolidayWallArt = 1,
    DoveCage = 2,
    JollyHolidayTree = 3
}

[SerializationGenerator(0, false)]
public partial class HolidayGiftToken2019 : Item, IRewardOption
{
    [SerializableField(0)]
    private HolidayGift2019 _gift;

    [Constructible]
    public HolidayGiftToken2019() : this(HolidayGift2019.None)
    {
    }

    [Constructible]
    public HolidayGiftToken2019(HolidayGift2019 gift) : base(0xA094)
    {
        LootType = LootType.Blessed;
        Light = LightType.Circle300;
        Weight = 1.0;
        _gift = gift;
        Hue = 133;
    }

    public override int LabelNumber => 1159257; // 2019 Holiday Gift Token

    public void GetOptions(RewardOptionList list)
    {
        list.Add((int)HolidayGift2019.HolidayWallArt, 1159254);
        list.Add((int)HolidayGift2019.DoveCage, 1159255);
        list.Add((int)HolidayGift2019.JollyHolidayTree, 1159256);
    }

    public void OnOptionSelected(Mobile from, int choice)
    {
        _gift = (HolidayGift2019)choice;

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
            case HolidayGift2019.JollyHolidayTree:
                {
                    box.DropItem(new JollyHolidayTreeDeed());
                    break;
                }
            case HolidayGift2019.DoveCage:
                {
                    box.DropItem(new DoveCage());
                    break;
                }
            case HolidayGift2019.HolidayWallArt:
                {
                    switch (Utility.Random(3))
                    {
                        case 0:
                            {
                                box.DropItem(new HolidayWallArt1());
                                break;
                            }
                        case 1:
                            {
                                box.DropItem(new HolidayWallArt2());
                                break;
                            }
                        case 2:
                            {
                                box.DropItem(new HolidayWallArt3());
                                break;
                            }
                    }

                    break;
                }
        }

        from.SendLocalizedMessage(1062306); // Your gift has been created and placed in your backpack.
        from.AddToBackpack(box);

        Delete();
    }
}
