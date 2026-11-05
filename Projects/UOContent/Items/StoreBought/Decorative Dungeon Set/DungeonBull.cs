using ModernUO.Serialization;
using Server.Gumps;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DungeonBullAddon : BaseAddon
{
    [Constructible]
    public DungeonBullAddon(bool east = true)
    {
        if (east)
        {
            AddComponent(new AddonComponent(42247), 0, 0, 0);
            AddComponent(new AddonComponent(42246), 0, 1, 0);
            AddComponent(new AddonComponent(42245), 1, 1, 0);
            AddComponent(new AddonComponent(42249), 1, 0, 0);
            AddComponent(new AddonComponent(42248), 1, -1, 0);
        }
        else
        {
            AddComponent(new AddonComponent(42237), 0, 0, 0);
            AddComponent(new AddonComponent(42236), 1, 0, 0);
            AddComponent(new AddonComponent(42235), 1, 1, 0);
            AddComponent(new AddonComponent(42239), 0, 1, 0);
            AddComponent(new AddonComponent(42238), -1, 1, 0);
        }
    }

    public override BaseAddonDeed Deed => new DungeonBullDeed();
}

[SerializationGenerator(0, false)]
public partial class DungeonBullDeed : BaseAddonDeed, IRewardOption
{
    [SerializableField(0)]
    private bool _east;

    [Constructible]
    public DungeonBullDeed()
    {
        LootType = LootType.Blessed;
    }

    public override int LabelNumber => 1159467; // Dungeon Bull
    public override BaseAddon Addon => new DungeonBullAddon(_east);

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
