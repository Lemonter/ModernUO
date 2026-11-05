using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/ExploringTheDeep/Items/QuestItems/AquaGem.cs). The gem the
///     Djinn drops for Zalia; it decays after an hour. The original added the "Quest Item" property by
///     hand in <c>GetProperties</c>; here the item is simply flagged <see cref="Item.QuestItem" />, which
///     produces the same property and lets the ML quest system's collect objectives see it.
/// </summary>
[SerializationGenerator(0, false)]
public partial class AquaGem : BaseDecayingItem
{
    [Constructible]
    public AquaGem() : base(0x4B48)
    {
        Stackable = false;
        Weight = 1.0;
        Hue = 1916;
        LootType = LootType.Blessed;
        QuestItem = true;
    }

    public override int LabelNumber => 1154244; // Aqua Gem

    public override int Lifespan => 3600;

    public override void OnDoubleClick(Mobile from)
    {
        base.OnDoubleClick(from);

        // *You hold the gem and admire its brilliance as it radiates blue beams of light*
        from.PublicOverheadMessage(MessageType.Regular, 0x3B2, 1154245);
    }
}
