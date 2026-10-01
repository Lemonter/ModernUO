using ModernUO.Serialization;

namespace Server.Items;

/// <summary>Ported from ServUO (Scripts/Services/Peerless/PeerlessKey.cs). Base for the items
/// that open a peerless encounter: the ingredient keys players collect from a dungeon's
/// minions, and the master keys the altar hands back once the offering is complete.
///
/// Blessed and week-long by design — a key is a raid ticket, not loot to be taken off a
/// corpse.
///
/// One adaptation: ServUO shows the facet through its ItemSocket framework
/// (AddItemSocketProperties), which doesn't exist in this codebase. The same clilocs are
/// written straight into GetProperties here, so the tooltip reads identically.</summary>
[SerializationGenerator(0, false)]
public abstract partial class PeerlessKey : BaseDecayingItem
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Map _peerlessMap;

    public PeerlessKey(int itemID) : base(itemID)
    {
        LootType = LootType.Blessed;
    }

    public override int Lifespan => 604800; // one week
    public override bool UseSeconds => false;

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        var cliloc = MapCliloc(_peerlessMap);

        if (cliloc != 0)
        {
            list.Add(cliloc);
        }
    }

    private static int MapCliloc(Map map)
    {
        if (map == Map.Felucca)
        {
            return 1012001; // Felucca
        }

        if (map == Map.Trammel)
        {
            return 1012000; // Trammel
        }

        if (map == Map.Ilshenar)
        {
            return 1012002; // Ilshenar
        }

        if (map == Map.Malas)
        {
            return 1060643; // Malas
        }

        if (map == Map.Tokuno)
        {
            return 1063258; // Tokuno Islands
        }

        return 0;
    }
}
