using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/ExploringTheDeep/Items/QuestItems/BrokenShipwreckRemains.cs).
///     Hauled out of Gravewater Lake with the special salvage hook and handed to Hepler Paulson.
/// </summary>
[SerializationGenerator(0, false)]
public partial class BrokenShipwreckRemains : BaseDecayingItem
{
    [Constructible]
    public BrokenShipwreckRemains() : this(1)
    {
    }

    [Constructible]
    public BrokenShipwreckRemains(int amount) : base(0xC2D)
    {
        Hue = 2969;
        Weight = 25.0;
        Amount = amount;
        LootType = LootType.Blessed;
        QuestItem = true;
    }

    public override int LabelNumber => 1154216; // Broken Remains of a Shipwreck

    public override int Lifespan => 3600;

    public override void OnDoubleClick(Mobile from)
    {
        // *You carefully examine the sodden remains of the wooden ship. You discern the ship was recently
        // foundered. You decide to show it to the Salvage Master*
        from.PublicOverheadMessage(MessageType.Regular, 0x559, 1154217);
    }
}
