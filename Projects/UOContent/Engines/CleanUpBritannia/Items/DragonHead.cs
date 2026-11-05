using ModernUO.Serialization;
using Server.Gumps;

namespace Server.Items;

public enum AddonFacing
{
    South = 0,
    East = 1
}

[SerializationGenerator(0, false)]
public partial class DragonHeadAddon : BaseAddon
{
    [SerializableField(0)]
    private AddonFacing _facing;

    [Constructible]
    public DragonHeadAddon() : this(AddonFacing.South)
    {
    }

    [Constructible]
    public DragonHeadAddon(AddonFacing facing)
    {
        _facing = facing;

        switch (facing)
        {
            case AddonFacing.East:
                {
                    AddComponent(new DragonHeadComponent(0x2235), 0, 0, 10);
                    break;
                }
            default:
                {
                    AddComponent(new DragonHeadComponent(0x2234), 0, 0, 10);
                    break;
                }
        }
    }

    public override BaseAddonDeed Deed => new DragonHeadAddonDeed();
}

[SerializationGenerator(0, false)]
public partial class DragonHeadComponent : AddonComponent
{
    [Constructible]
    public DragonHeadComponent() : this(0x2234)
    {
    }

    [Constructible]
    public DragonHeadComponent(int itemID) : base(itemID)
    {
    }

    public override bool NeedsWall => true;

    public override Point3D WallPosition => ItemID == 0x2235 ? new Point3D(-1, 0, 0) : new Point3D(0, -1, 0);
}

[SerializationGenerator(0, false)]
public partial class DragonHeadAddonDeed : BaseAddonDeed, IRewardOption
{
    [SerializableField(0)]
    private AddonFacing _facing;

    [Constructible]
    public DragonHeadAddonDeed()
    {
        LootType = LootType.Blessed;
    }

    public override BaseAddon Addon => new DragonHeadAddon(_facing);

    public override int LabelNumber => 1080209; // Dragon Head

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1062334); // This item must be in your backpack to be used.
            return;
        }

        RewardOptionGump.DisplayTo(from, this);
    }

    public void GetOptions(RewardOptionList list)
    {
        list.Add((int)AddonFacing.South, 1080208);
        list.Add((int)AddonFacing.East, 1080207);
    }

    public void OnOptionSelected(Mobile from, int choice)
    {
        _facing = (AddonFacing)choice;

        if (!Deleted)
        {
            base.OnDoubleClick(from);
        }
    }
}
