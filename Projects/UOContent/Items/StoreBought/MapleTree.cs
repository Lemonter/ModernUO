using ModernUO.Serialization;
using Server.Gumps;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class MapleTreeAddon : BaseAddon
{
    [Constructible]
    public MapleTreeAddon(bool trunk = false)
    {
        AddComponent(new LocalizedAddonComponent(0x247D, 1071104), 0, 0, 0);

        if (!trunk)
        {
            AddComponent(new LocalizedAddonComponent(0x36A1, 1071104), 0, 0, 0);
        }
    }

    public override BaseAddonDeed Deed => new MapleTreeDeed();
}

[SerializationGenerator(0, false)]
public partial class MapleTreeDeed : BaseAddonDeed, IRewardOption
{
    [SerializableField(0)]
    private bool _trunk;

    [Constructible]
    public MapleTreeDeed()
    {
        LootType = LootType.Blessed;
    }

    public override BaseAddon Addon => new MapleTreeAddon(_trunk);
    public override int LabelNumber => 1071104; // Maple Tree

    public void GetOptions(RewardOptionList list)
    {
        list.Add(0, 1071104); // Maple Tree
        list.Add(1, 1071301); // Maple Tree (trunk)
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
