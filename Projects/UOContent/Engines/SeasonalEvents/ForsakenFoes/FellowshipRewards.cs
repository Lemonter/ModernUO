using System.Collections.Generic;
using Server.Engines.VvV;
using Server.Items;

namespace Server.Engines.SeasonalEvents;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/ForsakenFoes/Rewards.cs).
///     Dropped: <c>PumpkinDeed</c> and <c>PumpkinRowBoatDeed</c> (High Seas ship cannons and boats — <c>ShipCannonDeed</c>,
///     <c>CannonPower</c>, <c>BaseShipCannon</c> and <c>BaseGalleon</c> do not exist here) and the six
///     <c>RecipeScroll</c> entries (ServUO's <c>TailorRecipe</c> ids for the fellowship belts/hoods are not defined in
///     this codebase's craft system).
/// </summary>
public static class FellowshipRewards
{
    public static List<CollectionItem> Rewards { get; } = new()
    {
        new CollectionItem(typeof(TinctureOfSilver), 0x183B, 1155619, 1900, 10000),
        new CollectionItem(typeof(JackOLanternHelm), 0xA3EA, 1125986, 0, 150000),
        new CollectionItem(typeof(AdmiralJacksPumpkinSpiceAle), 0xA40B, 1159230, 1922, 50000),
        new CollectionItem(typeof(ExplodingJackOLantern), 0xA407, 1159220, 1175, 40000),
        new CollectionItem(typeof(CaptainTitleDeed), 0x14EF, 1156995, 0, 80000),
        new CollectionItem(typeof(CommanderTitleDeed), 0x14EF, 1156995, 0, 40000),
        new CollectionItem(typeof(EnsignTitleDeed), 0x14EF, 1156995, 0, 20000)
    };
}
