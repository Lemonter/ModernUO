using ModernUO.Serialization;
using Server.Gumps;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class SmallWorldTreeRugAddon : BaseAddon
{
    private static readonly int[,] _southInfo =
    {
        { 40514, 1, 1, 0 }, { 40513, 1, 0, 0 }, { 40512, 1, -1, 0 },
        { 40511, 0, -1, 0 }, { 40510, -1, -1, 0 }, { 40509, -1, 0, 0 },
        { 40508, -1, 1, 0 }, { 40507, 0, 1, 0 }, { 40506, 0, 0, 0 }
    };

    private static readonly int[,] _eastInfo =
    {
        { 40523, 1, 1, 0 }, { 40522, 0, 1, 0 }, { 40521, -1, 1, 0 },
        { 40520, -1, 0, 0 }, { 40519, -1, -1, 0 }, { 40518, 0, -1, 0 },
        { 40517, 1, -1, 0 }, { 40516, 1, 0, 0 }, { 40515, 0, 0, 0 }
    };

    [Constructible]
    public SmallWorldTreeRugAddon(bool south = true)
    {
        var info = south ? _southInfo : _eastInfo;

        for (var i = 0; i < info.Length / 4; i++)
        {
            AddComponent(new AddonComponent(info[i, 0]), info[i, 1], info[i, 2], info[i, 3]);
        }
    }

    public override BaseAddonDeed Deed => new SmallWorldTreeRugAddonDeed();
}

[SerializationGenerator(0, false)]
public partial class SmallWorldTreeRugAddonDeed : BaseAddonDeed, IRewardOption
{
    [SerializableField(0, setter: "private")]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _south;

    [Constructible]
    public SmallWorldTreeRugAddonDeed()
    {
    }

    public override BaseAddon Addon => new SmallWorldTreeRugAddon(_south);
    public override int LabelNumber => 1157206; // Small World Tree

    public void GetOptions(RewardOptionList list)
    {
        list.Add(0, 1116332); // South
        list.Add(1, 1116333); // East
    }

    public void OnOptionSelected(Mobile from, int choice)
    {
        _south = choice == 0;

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

[SerializationGenerator(0, false)]
public partial class LargeWorldTreeRugAddon : BaseAddon
{
    private static readonly int[,] _southInfo =
    {
        { 40546, -2, -1, 0 }, { 40545, -1, -1, 0 }, { 40544, -2, 0, 0 },
        { 40543, -1, 0, 0 }, { 40542, -2, 1, 0 }, { 40541, -1, 1, 0 },
        { 40540, -2, 2, 0 }, { 40539, -1, 2, 0 }, { 40536, 0, -1, 0 },
        { 40534, 1, -1, 0 }, { 40533, 2, -1, 0 }, { 40532, 0, 0, 0 },
        { 40531, 1, 0, 0 }, { 40530, 0, 1, 0 }, { 40529, 2, 0, 0 },
        { 40528, 1, 1, 0 }, { 40527, 0, 2, 0 }, { 40526, 2, 1, 0 },
        { 40525, 1, 2, 0 }, { 40547, -1, -2, 0 }, { 40538, 0, -2, 0 },
        { 40537, 1, -2, 0 }, { 40535, 2, -2, 0 }
    };

    private static readonly int[,] _eastInfo =
    {
        { 40572, -2, -1, 0 }, { 40571, -1, -2, 0 }, { 40570, -1, -1, 0 },
        { 40569, 0, -2, 0 }, { 40568, 0, -1, 0 }, { 40567, 1, -2, 0 },
        { 40566, 1, -1, 0 }, { 40565, 2, -2, 0 }, { 40564, 2, -1, 0 },
        { 40563, -2, 0, 0 }, { 40562, -2, 1, 0 }, { 40561, -1, 0, 0 },
        { 40560, -2, 2, 0 }, { 40559, -1, 1, 0 }, { 40558, -1, 2, 0 },
        { 40557, 0, 0, 0 }, { 40556, 0, 1, 0 }, { 40555, 1, 0, 0 },
        { 40551, 1, 2, 0 }, { 40554, 0, 2, 0 }, { 40553, 1, 1, 0 },
        { 40552, 2, 0, 0 }, { 40550, 2, 1, 0 }
    };

    [Constructible]
    public LargeWorldTreeRugAddon(bool south = true)
    {
        var info = south ? _southInfo : _eastInfo;

        for (var i = 0; i < info.Length / 4; i++)
        {
            AddComponent(new AddonComponent(info[i, 0]), info[i, 1], info[i, 2], info[i, 3]);
        }
    }

    public override BaseAddonDeed Deed => new LargeWorldTreeRugAddonDeed();
}

[SerializationGenerator(0, false)]
public partial class LargeWorldTreeRugAddonDeed : BaseAddonDeed, IRewardOption
{
    [SerializableField(0, setter: "private")]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _south;

    [Constructible]
    public LargeWorldTreeRugAddonDeed()
    {
    }

    public override BaseAddon Addon => new LargeWorldTreeRugAddon(_south);
    public override int LabelNumber => 1157207; // Large World Tree

    public void GetOptions(RewardOptionList list)
    {
        list.Add(0, 1116332); // South
        list.Add(1, 1116333); // East
    }

    public void OnOptionSelected(Mobile from, int choice)
    {
        _south = choice == 0;

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
