using System.Collections.Generic;
using Server.Engines.VvV;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.SeasonalEvents;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/RisingTide/RewardsGump.cs).
///     Dropped: the <c>BlundercannonDeed</c> entry — it is a High Seas ship-cannon deed and that system does not
///     exist here. The rest of the list is unchanged.
/// </summary>
public static class RisingTideRewards
{
    public static List<CollectionItem> Rewards { get; } = new()
    {
        new CollectionItem(typeof(DragonCannonDeed), 0x14EF, 1158926, 0, 120000),
        new CollectionItem(typeof(PirateWallMap), 0xA2C8, 1158938, 0, 45000),
        new CollectionItem(typeof(MysteriousStatue), 0xA2C6, 1158935, 0, 35000),
        new CollectionItem(typeof(ShoulderParrot), 0xA2CA, 1158928, 0, 100000),
        new CollectionItem(typeof(DecorativeWoodCarving), 0x4C26, 1158943, 2968, 15000),
        new CollectionItem(typeof(QuartermasterRewardDeed), 0x14EF, 0, 0, 25000),
        new CollectionItem(typeof(SailingMasterRewardDeed), 0x14EF, 0, 0, 20000),
        new CollectionItem(typeof(BotswainRewardDeed), 0x14EF, 0, 0, 15000),
        new CollectionItem(typeof(PowderMonkeyRewardDeed), 0x14EF, 0, 0, 10000),
        new CollectionItem(typeof(SpikedWhipOfPlundering), 0xA28B, 0, 0, 180000),
        new CollectionItem(typeof(BladedWhipOfPlundering), 0xA28B, 0, 0, 180000),
        new CollectionItem(typeof(BarbedWhipOfPlundering), 0xA28B, 0, 0, 180000),
        new CollectionItem(typeof(TritonStatue), 0xA2D8, 0, 2713, 140000)
    };
}
