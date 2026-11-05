using ModernUO.Serialization;
using Server.Engines.VeteranRewards;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class AllegiancePouch : Backpack, IRewardItem
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _isRewardItem;

    [Constructible]
    public AllegiancePouch()
    {
        Hue = 2958;
        Weight = 1.0;
        LootType = LootType.Regular;

        DropItem(new OrderBanner());
        DropItem(new ChaosBanner());
    }

    public override int LabelNumber => 1113953; // Allegiance Pouch

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (_isRewardItem)
        {
            list.Add(1113802); // 12th Year Veteran Reward
        }
    }
}
