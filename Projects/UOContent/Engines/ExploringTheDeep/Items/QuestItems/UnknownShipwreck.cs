using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/ExploringTheDeep/Items/QuestItems/UnknownShipwreck.cs). The map
///     Hepler Paulson hands over once the whole suit has been assembled; reading it points the player at
///     Gravewater Lake.
/// </summary>
[SerializationGenerator(0, false)]
public partial class UnknownShipwreck : BaseDecayingItem
{
    [Constructible]
    public UnknownShipwreck() : base(0x14ED)
    {
        Weight = 1.0;
        LootType = LootType.Blessed;
        QuestItem = true;
    }

    public override int LabelNumber => 1154269; // Map to an Unknown Shipwreck

    public override int Lifespan => 3600;

    public override void OnDoubleClick(Mobile from)
    {
        if (from.InRange(GetWorldLocation(), 2))
        {
            // *You unfurl the map and study it carefully. You recognize Gravewater Lake. In the center of
            // the lake is a large X*
            from.PublicOverheadMessage(MessageType.Regular, 0x22, 1154270);
        }
        else
        {
            from.SendLocalizedMessage(500446); // That is too far away.
        }
    }
}
