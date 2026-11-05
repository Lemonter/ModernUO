using ModernUO.Serialization;
using System;
using Server.Gumps;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/Shadow Furniture Set/ShadowAltar.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class ShadowAltarAddon : BaseAddon
{
    public ShadowAltarAddon(DirectionType type)
    {
        switch (type)
        {
            case DirectionType.South:
                AddComponent(new LocalizedAddonComponent(0x365A, 1076682), 1, 0, 0);
                AddComponent(new LocalizedAddonComponent(0x365B, 1076682), 0, 0, 0);
                break;
            case DirectionType.East:
                AddComponent(new LocalizedAddonComponent(0x369E, 1076682), 0, 1, 0);
                AddComponent(new LocalizedAddonComponent(0x369D, 1076682), 0, 0, 0);
                break;
        }
    }

    public override BaseAddonDeed Deed => new ShadowAltarDeed();
}

[SerializationGenerator(0, false)]
public partial class ShadowAltarDeed : BaseAddonDeed, IRewardOption
{
    public override int LabelNumber => 1076682; // Shadow Altar

    public override BaseAddon Addon => new ShadowAltarAddon(_Direction);

    private DirectionType _Direction;

    [Constructible]
    public ShadowAltarDeed()
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
            RewardOptionGump.DisplayTo(from, this, 1076783); // Please select your shadow altar position
        }
        else
        {
            from.SendLocalizedMessage(1062334); // This item must be in your backpack to be used.
        }
    }
}
