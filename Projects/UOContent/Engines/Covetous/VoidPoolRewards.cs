using System.Collections.Generic;
using Server.Engines.VvV;
using Server.Items;

namespace Server.Engines.VoidPool;

/// <summary>
///     Ported from ServUO's Covetous void pool (Items/VoidPoolRewards.cs). ServUO's reward store came
///     from the generic <c>Engines.Points</c> framework, which this codebase doesn't have; the list is
///     built from the standalone <see cref="CollectionItem" /> the VvV port already carries, and the
///     balance lives in <see cref="VoidPoolController.GetPoints" />.
///
///     Only the rewards ported so far are listed — the transmutation cauldron, harvest maps and
///     temporary forge join this table once their items land.
/// </summary>
public static class VoidPoolRewards
{
    public static List<CollectionItem> Rewards { get; } = new()
    {
        new CollectionItem(typeof(CauldronOfTransmutationDeed), 0x9ED, 1152656, (int)CraftResource.Bronze, 100),
        new CollectionItem(typeof(CauldronOfTransmutationDeed), 0x9ED, 1152657, (int)CraftResource.Gold, 200),
        new CollectionItem(typeof(CauldronOfTransmutationDeed), 0x9ED, 1152658, (int)CraftResource.Agapite, 500),
        new CollectionItem(typeof(CauldronOfTransmutationDeed), 0x9ED, 1152659, (int)CraftResource.Verite, 1000),

        new CollectionItem(typeof(CauldronOfTransmutationDeed), 0x9ED, 1152660, (int)CraftResource.AshWood, 100),
        new CollectionItem(typeof(CauldronOfTransmutationDeed), 0x9ED, 1152661, (int)CraftResource.YewWood, 200),
        new CollectionItem(typeof(CauldronOfTransmutationDeed), 0x9ED, 1152662, (int)CraftResource.Heartwood, 500),
        new CollectionItem(typeof(CauldronOfTransmutationDeed), 0x9ED, 1152663, (int)CraftResource.Bloodwood, 1000),

        new CollectionItem(typeof(HarvestMap), 0x14EC, 1152665, (int)CraftResource.Gold, 250),
        new CollectionItem(typeof(HarvestMap), 0x14EC, 1152666, (int)CraftResource.Agapite, 500),
        new CollectionItem(typeof(HarvestMap), 0x14EC, 1152667, (int)CraftResource.Verite, 1000),
        new CollectionItem(typeof(HarvestMap), 0x14EC, 1152668, (int)CraftResource.Valorite, 2000),
        new CollectionItem(typeof(HarvestMap), 0x14EC, 1152669, (int)CraftResource.YewWood, 250),
        new CollectionItem(typeof(HarvestMap), 0x14EC, 1152670, (int)CraftResource.Heartwood, 500),
        new CollectionItem(typeof(HarvestMap), 0x14EC, 1152671, (int)CraftResource.Bloodwood, 1000),
        new CollectionItem(typeof(HarvestMap), 0x14EC, 1152672, (int)CraftResource.Frostwood, 2000),

        new CollectionItem(typeof(SmeltersTalisman), 0x2F5B, 1152674, (int)CraftResource.Gold, 50),
        new CollectionItem(typeof(SmeltersTalisman), 0x2F5B, 1152675, (int)CraftResource.Agapite, 100),
        new CollectionItem(typeof(SmeltersTalisman), 0x2F5B, 1152676, (int)CraftResource.Verite, 200),
        new CollectionItem(typeof(SmeltersTalisman), 0x2F5B, 1152677, (int)CraftResource.Valorite, 500),

        new CollectionItem(typeof(WoodsmansTalisman), 0x2F5A, 1152678, (int)CraftResource.YewWood, 50),
        new CollectionItem(typeof(WoodsmansTalisman), 0x2F5A, 1152679, (int)CraftResource.Heartwood, 100),
        new CollectionItem(typeof(WoodsmansTalisman), 0x2F5A, 1152680, (int)CraftResource.Bloodwood, 200),
        new CollectionItem(typeof(WoodsmansTalisman), 0x2F5A, 1152681, (int)CraftResource.Frostwood, 500),

        new CollectionItem(typeof(TemporaryForgeDeed), 0xFB1, 1152682, 0, 250),
        new CollectionItem(typeof(MagicalFishFinder), 0x14F6, 1152683, 2500, 250),

        new CollectionItem(typeof(BraceletOfProtection), 0x1086, 1152730, 1157, 1840),
        new CollectionItem(typeof(Hephaestus), 0x1B76, 1152909, 1910, 2000),
        new CollectionItem(typeof(BlightOfTheTundra), 0x26C2, 1152910, 1165, 2515),
        new CollectionItem(typeof(Brightblade), 0x13FF, 1152732, 1756, 2500)
    };

    public static Item DropRandomArtifact() => Utility.Random(4) switch
    {
        0 => new Brightblade(),
        1 => new BraceletOfProtection(),
        2 => new BlightOfTheTundra(),
        3 => new Hephaestus(),
        _ => null
    };
}
