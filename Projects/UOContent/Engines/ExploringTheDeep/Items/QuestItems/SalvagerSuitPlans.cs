using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/ExploringTheDeep/Items/QuestItems/SalvagerSuitPlans.cs). The
///     plans from the locked strongbox in the Sorcerer's Dungeon; Cousteau Perron takes them and hands
///     back the list of craftsmen.
/// </summary>
[SerializationGenerator(0, false)]
public partial class SalvagerSuitPlans : BaseDecayingItem
{
    [Constructible]
    public SalvagerSuitPlans() : this(1)
    {
    }

    [Constructible]
    public SalvagerSuitPlans(int amount) : base(0x2258)
    {
        Hue = 92;
        Stackable = false;
        Weight = 1.0;
        Amount = amount;
        LootType = LootType.Blessed;
        QuestItem = true;
    }

    public override int LabelNumber => 1154229; // Plans to A Salvager Suit

    public override int Lifespan => 18000;
    public override bool UseSeconds => false;

    public override void OnDoubleClick(Mobile from)
    {
        base.OnDoubleClick(from);

        // *You examine the document carefully. It appears to be the detailed schematic of some kind of
        // suit. It is beyond your understanding. You decide to take it back to the Master Tinker*
        from.PublicOverheadMessage(MessageType.Regular, 0x3B2, 1154230);
    }
}
