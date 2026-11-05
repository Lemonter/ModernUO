using ModernUO.Serialization;
using Server.Gumps;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class HildebrandtDragonRugAddon : BaseAddon
{
    private static readonly int[,] _southInfo =
    {
        { 40909, -2, -2, 0 }, { 40908, -2, -1, 0 }, { 40898, -1, -1, 0 },
        { 40897, -1, 0, 0 }, { 40896, -1, 1, 0 }, { 40895, -1, 2, 0 },
        { 40887, 1, 0, 0 }, { 40886, 0, 1, 0 }, { 40885, 1, 1, 0 },
        { 40904, 2, -2, 0 }, { 40894, 1, 2, 0 }, { 40888, 0, 0, 0 },
        { 40891, 1, -2, 0 }, { 40892, 0, -2, 0 }, { 40893, 0, 2, 0 },
        { 40899, -1, -2, 0 }, { 40900, 2, 2, 0 }, { 40901, 2, 1, 0 },
        { 40902, 2, 0, 0 }, { 40903, 2, -1, 0 }, { 40889, 1, -1, 0 },
        { 40890, 0, -1, 0 }, { 40905, -2, 2, 0 }, { 40906, -2, 1, 0 },
        { 40907, -2, 0, 0 }
    };

    private static readonly int[,] _eastInfo =
    {
        { 40926, 2, 1, 0 }, { 40918, 0, 2, 0 }, { 40919, 1, 2, 0 },
        { 40920, -1, 2, 0 }, { 40910, 1, 1, 0 }, { 40911, 0, 1, 0 },
        { 40930, -2, 2, 0 }, { 40921, -1, 1, 0 }, { 40931, -2, 1, 0 },
        { 40925, 2, 2, 0 }, { 40914, 1, -1, 0 }, { 40915, 0, -1, 0 },
        { 40916, 0, -2, 0 }, { 40917, 1, -2, 0 }, { 40913, 0, 0, 0 },
        { 40934, -2, -2, 0 }, { 40927, 2, 0, 0 }, { 40932, -2, -1, 0 },
        { 40933, -2, 0, 0 }, { 40922, -1, 0, 0 }, { 40923, -1, -1, 0 },
        { 40928, 2, -1, 0 }, { 40929, 2, -2, 0 }, { 40924, -1, -2, 0 },
        { 40912, 1, 0, 0 }
    };

    [Constructible]
    public HildebrandtDragonRugAddon(bool south = true)
    {
        var info = south ? _southInfo : _eastInfo;

        for (var i = 0; i < info.Length / 4; i++)
        {
            AddComponent(new AddonComponent(info[i, 0]), info[i, 1], info[i, 2], info[i, 3]);
        }
    }

    public override BaseAddonDeed Deed => new HildebrandtDragonRugDeed();
}

[SerializationGenerator(0, false)]
public partial class HildebrandtDragonRugDeed : BaseAddonDeed, IRewardOption
{
    [SerializableField(0, setter: "private")]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _south;

    [Constructible]
    public HildebrandtDragonRugDeed()
    {
    }

    public override BaseAddon Addon => new HildebrandtDragonRugAddon(_south);
    public override int LabelNumber => 1157889; // Hildebrandt Dragon Rug

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
