using ModernUO.Serialization;
using Server.Engines.VeteranRewards;

namespace Server.Items;

[Flippable(0x42CD, 0x42CE)]
[SerializationGenerator(0, false)]
public partial class OrderBanner : Item, IRewardItem
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _isRewardItem;

    [Constructible]
    public OrderBanner() : base(0x42CD)
    {
        Name = "знамя Порядка";
        Weight = 1.0;
        LootType = LootType.Blessed;
    }
}

[Flippable(0x42CB, 0x42CC)]
[SerializationGenerator(0, false)]
public partial class ChaosBanner : Item, IRewardItem
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _isRewardItem;

    [Constructible]
    public ChaosBanner() : base(0x42CB)
    {
        Name = "знамя Хаоса";
        Weight = 1.0;
        LootType = LootType.Blessed;
    }
}
