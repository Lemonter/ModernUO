using System;

namespace Server.Engines.VvV;

// ServUO's real CollectionItem comes from a generic "collection/reward store" framework used
// by several unrelated ServUO systems — that framework doesn't exist in this codebase, so this
// is a minimal standalone version with just what VvVRewards/SilverTrader/VvVRewardGump need.
public class CollectionItem
{
    public Type Type { get; }
    public int ItemID { get; }
    public int Tooltip { get; }
    public int Hue { get; }
    public int Price { get; }

    public CollectionItem(Type type, int itemId, int tooltip, int hue, int price)
    {
        Type = type;
        ItemID = itemId;
        Tooltip = tooltip;
        Hue = hue;
        Price = price;
    }
}
