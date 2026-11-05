using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/ExploringTheDeep/Items/QuestItems/AquaPendant.cs). One of the
///     four pieces of the Salvager's Suit, made by Zalia from the Aqua Gem.
/// </summary>
[SerializationGenerator(0, false)]
public partial class AquaPendant : GoldNecklace
{
    [Constructible]
    public AquaPendant()
    {
        Hue = 1916;
        LootType = LootType.Blessed;
        QuestItem = true;
    }

    public override int LabelNumber => 1154246; // Aqua Pendant

    public override void OnDoubleClick(Mobile from)
    {
        base.OnDoubleClick(from);

        // *As you hold the pendant in your hands you suddenly feel as though you no longer need to
        // breathe. The pendant pulses with magical energy!*
        from.PublicOverheadMessage(MessageType.Regular, 0x3B2, 1154247);
    }
}
