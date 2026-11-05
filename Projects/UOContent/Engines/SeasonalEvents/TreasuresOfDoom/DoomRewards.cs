using System.Collections.Generic;
using Server.Engines.VvV;
using Server.Items;

namespace Server.Engines.SeasonalEvents;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfDoom/Rewards.cs).
///     ServUO filled the list from an Initialize() hook; here it is a static lazy list, the same shape the
///     Blackthorn and VoidPool ports use (<see cref="CollectionItem" /> comes from the VvV port because ServUO's
///     generic collection framework does not exist here).
/// </summary>
public static class DoomRewards
{
    public static List<CollectionItem> Rewards { get; } = new()
    {
        new CollectionItem(typeof(LanternOfLight), 0xA25, 1155598, 0, 30),
        new CollectionItem(typeof(TinctureOfSilver), 0x183B, 1155619, 1900, 10),
        new CollectionItem(typeof(AntiqueDocumentsKit), 0x1EBB, 1155630, 0, 10),
        new CollectionItem(typeof(TreasuresOfDoomRewardDeed), 0x14EF, 1155603, 0, 20),
        new CollectionItem(typeof(TreasuresOfDoomRewardDeed), 0x14EF, 1155600, 0, 30),
        new CollectionItem(typeof(TreasuresOfDoomRewardDeed), 0x14EF, 1155602, 0, 50),
        new CollectionItem(typeof(CrookOfHumility), 0xE81, 1155624, 0, 50),
        new CollectionItem(typeof(ScepterOfPride), 0x26BC, 1155623, 0, 50),
        new CollectionItem(typeof(CloakOfLight), 0x1515, 1155608, 0, 50),
        new CollectionItem(typeof(BootsOfEscaping), 0x1711, 1155607, 0, 50),
        new CollectionItem(typeof(SterlingSilverRing), 0x1F09, 1155606, 0, 50)
    };
}
