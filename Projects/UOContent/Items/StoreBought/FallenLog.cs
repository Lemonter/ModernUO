using ModernUO.Serialization;
using Server.Gumps;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class FallenLogAddon : BaseAddon
{
    [Constructible]
    public FallenLogAddon(bool east = true)
    {
        if (east)
        {
            AddComponent(new AddonComponent(0x0CF5), -1, 0, 0);
            AddComponent(new AddonComponent(0x0CF6), 0, 0, 0);
            AddComponent(new AddonComponent(0x0CF7), 1, 0, 0);
        }
        else
        {
            AddComponent(new AddonComponent(0x0CF4), 0, 0, 0);
            AddComponent(new AddonComponent(0x0CF3), 0, -1, 0);
        }
    }

    public override BaseAddonDeed Deed => new FallenLogDeed();
}

[SerializationGenerator(0, false)]
public partial class FallenLogDeed : BaseAddonDeed, IRewardOption
{
    [SerializableField(0)]
    private bool _east;

    [Constructible]
    public FallenLogDeed()
    {
        LootType = LootType.Blessed;
    }

    public override int LabelNumber => 1071088; // Fallen Log
    public override BaseAddon Addon => new FallenLogAddon(_east);

    public void GetOptions(RewardOptionList list)
    {
        list.Add(0, 1116332); // South
        list.Add(1, 1116333); // East
    }

    public void OnOptionSelected(Mobile from, int choice)
    {
        _east = choice == 1;

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
