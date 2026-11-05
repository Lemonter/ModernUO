using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     Ported from ServUO (Scripts/Services/ExploringTheDeep/Questers/SBHepler.cs). The Salvage Master's
///     stock: one special salvage hook. ModernUO's <c>GenericBuyInfo</c> takes the item type and its
///     graphic directly rather than ServUO's cliloc-name string overload.
/// </summary>
public class SBHepler : SBInfo
{
    public override IShopSellInfo SellInfo { get; } = new InternalSellInfo();

    public override List<GenericBuyInfo> BuyInfo { get; } = new InternalBuyInfo();

    public class InternalBuyInfo : List<GenericBuyInfo>
    {
        public InternalBuyInfo()
        {
            Add(new GenericBuyInfo(typeof(SpecialSalvageHook), 1900, 10, 0x14F7, 2654));
        }
    }

    public class InternalSellInfo : GenericSellInfo
    {
    }
}
