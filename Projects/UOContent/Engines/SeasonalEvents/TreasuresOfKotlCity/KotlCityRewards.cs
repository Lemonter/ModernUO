using System.Collections.Generic;
using Server.Engines.VvV;
using Server.Items;

namespace Server.Engines.SeasonalEvents;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfKotlCity/Rewards.cs).
///     Dropped: the three <c>RecipeScroll</c> entries (the Kotl recipes behind <c>CarpRecipes.KotlBlackRod</c>,
///     <c>TinkerRecipes.DrSpectorLenses</c> and <c>TinkerRecipes.KotlAutomatonHead</c> are not defined in this
///     codebase's craft system). <see cref="BlackrockMoonstone" /> and <see cref="AutomatonActuator" /> are ported
///     with the pack because the reward list needs them.
/// </summary>
public static class KotlCityRewards
{
    public static List<CollectionItem> Rewards { get; } = new()
    {
        new CollectionItem(typeof(SkeletalHangmanAddonDeed), 0x14EF, 1156982, 0, 10),
        new CollectionItem(typeof(KotlSacraficialAltarAddonDeed), 0x14EF, 1124311, 0, 10),
        new CollectionItem(typeof(BlackrockMoonstone), 0x9CAA, 1156993, 1175, 10),
        new CollectionItem(typeof(AutomatonActuator), 0x9CE9, 1156997, 0, 5),
        new CollectionItem(typeof(TreasuresOfKotlRewardDeed), 0x14EF, 1156987, 0, 10),
        new CollectionItem(typeof(TreasuresOfKotlRewardDeed), 0x14EF, 1156986, 0, 10),
        new CollectionItem(typeof(TreasuresOfKotlRewardDeed), 0x14EF, 1156985, 0, 10),
        new CollectionItem(typeof(TribalBanner), 0x9D5E, 1124309, 0, 20),
        new CollectionItem(typeof(TribalBanner), 0x9D5C, 1124307, 0, 20),
        new CollectionItem(typeof(TribalBanner), 0x9D5A, 1124305, 0, 20),
        new CollectionItem(typeof(TribalBanner), 0x9D58, 1124303, 0, 20),
        new CollectionItem(typeof(TribalBanner), 0x9D56, 1124301, 0, 20),
        new CollectionItem(typeof(TribalBanner), 0x9D54, 1124299, 0, 20),
        new CollectionItem(typeof(KatalkotlsRing), 0x1F09, 1156989, 2591, 50),
        new CollectionItem(typeof(TalonsOfEscaping), 0x41D8, 1155682, 0, 50),
        new CollectionItem(typeof(BootsOfEscaping), 0x1711, 1155607, 0, 50)
    };
}
