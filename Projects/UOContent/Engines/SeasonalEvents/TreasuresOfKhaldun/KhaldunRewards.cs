using System.Collections.Generic;
using Server.Engines.VvV;
using Server.Items;

namespace Server.Engines.SeasonalEvents;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfKhaldun/KhaldunRewards.cs).
///     ServUO filled the list from an <c>Initialize()</c> hook; here it is a static list, the same shape the Doom,
///     Blackthorn and VoidPool ports use (<see cref="CollectionItem" /> comes from the VvV port because ServUO's
///     generic collection framework does not exist here). ServUO's <c>IsTokunoDyable</c> helper had no callers and
///     is dropped.
/// </summary>
public static class KhaldunRewards
{
    public static List<CollectionItem> Rewards { get; } = new()
    {
        new CollectionItem(typeof(KhaldunFirstAidBelt), 0xA1F6, 1158681, 0, 30),
        new CollectionItem(typeof(MaskOfKhalAnkur), 0xA1C7, 1158701, 0, 50),
        new CollectionItem(typeof(PendantOfKhalAnkur), 0xA1C9, 1158731, 0, 50),
        new CollectionItem(typeof(SeekerOfTheFallenStarTitleDeed), 5360, 1155604, 0, 20),
        new CollectionItem(typeof(ZealotOfKhalAnkurTitleDeed), 5360, 1155604, 0, 30),
        new CollectionItem(typeof(ProphetTitleDeed), 5360, 1155604, 0, 50),
        new CollectionItem(typeof(CultistsRitualTome), 0xEFA, 1158717, 0, 50),
        new CollectionItem(typeof(SterlingSilverRing), 0x1F09, 1155606, 0, 50),
        new CollectionItem(typeof(TalonsOfEscaping), 0x41D8, 1155682, 0, 50),
        new CollectionItem(typeof(BootsOfEscaping), 0x1711, 1155607, 0, 50)
    };
}
