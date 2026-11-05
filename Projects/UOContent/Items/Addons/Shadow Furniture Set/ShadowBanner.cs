using ModernUO.Serialization;
using System;
using Server.Gumps;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/Shadow Furniture Set/ShadowBanner.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class ShadowBannerAddon : BaseAddon
{
    public ShadowBannerAddon(DirectionType type)
    {
        switch (type)
        {
            case DirectionType.South:
                AddComponent(new LocalizedAddonComponent(0x365C, 1076683), 1, 0, 0);
                AddComponent(new LocalizedAddonComponent(0x365D, 1076683), 0, 0, 0);
                break;
            case DirectionType.East:
                AddComponent(new LocalizedAddonComponent(0x365E, 1076683), 0, 0, 0);
                AddComponent(new LocalizedAddonComponent(0x365F, 1076683), 0, 1, 0);
                break;
        }
    }

    public override BaseAddonDeed Deed => new ShadowBannerDeed();
}

[SerializationGenerator(0, false)]
public partial class ShadowBannerDeed : BaseAddonDeed, IRewardOption
{
    public override int LabelNumber => 1076683; // Shadow Banner

    public override BaseAddon Addon => new ShadowBannerAddon(_Direction);

    private DirectionType _Direction;

    [Constructible]
    public ShadowBannerDeed()
        : base()
    {
        LootType = LootType.Blessed;
        Hue = 1908;
    }

    public void GetOptions(RewardOptionList list)
    {
        list.Add((int)DirectionType.South, 1075386); // South
        list.Add((int)DirectionType.East, 1075387); // East
    }

    public void OnOptionSelected(Mobile from, int choice)
    {
        _Direction = (DirectionType)choice;

        if (!Deleted)
            base.OnDoubleClick(from);
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (IsChildOf(from.Backpack))
        {
            from.CloseGump<RewardOptionGump>();
            RewardOptionGump.DisplayTo(from, this, 1076728); // Please select your shadow banner position:
        }
        else
        {
            from.SendLocalizedMessage(1062334); // This item must be in your backpack to be used.
        }
    }
}
