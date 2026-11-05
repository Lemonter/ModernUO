using ModernUO.Serialization;
using Server.Gumps;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class SnowTreeAddon : BaseAddon
{
    [Constructible]
    public SnowTreeAddon(bool trunk = false)
    {
        AddComponent(new LocalizedAddonComponent(0xDA0, 1071103), 0, 0, 0);

        if (!trunk)
        {
            var comp = new LocalizedAddonComponent(0xD9D, 1071103);
            comp.Hue = 1153;
            AddComponent(comp, 0, 0, 0);
        }
    }

    public override BaseAddonDeed Deed => new SnowTreeDeed();
}

[SerializationGenerator(0, false)]
public partial class SnowTreeDeed : BaseAddonDeed, IRewardOption
{
    [SerializableField(0)]
    private bool _trunk;

    [Constructible]
    public SnowTreeDeed()
    {
        LootType = LootType.Blessed;
    }

    public override BaseAddon Addon => new SnowTreeAddon(_trunk);
    public override int LabelNumber => 1071103; // Snow Tree

    public void GetOptions(RewardOptionList list)
    {
        list.Add(0, 1071103); // Snow Tree
        list.Add(1, 1071300); // Snow Tree (trunk)
    }

    public void OnOptionSelected(Mobile from, int choice)
    {
        _trunk = choice == 1;

        if (!Deleted)
        {
            base.OnDoubleClick(from);
        }
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1062334); // This item must be in your backpack to be used.
            return;
        }

        RewardOptionGump.DisplayTo(from, this);
    }
}
