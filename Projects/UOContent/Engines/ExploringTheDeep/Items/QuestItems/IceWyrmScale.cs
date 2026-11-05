using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/ExploringTheDeep/Items/QuestItems/IceWyrmScale.cs). Cut from
///     the Ice Wyrm in the Ice Dungeon and carried back to Cousteau Perron.
/// </summary>
[SerializationGenerator(0, false)]
public partial class IceWyrmScale : BaseDecayingItem
{
    [Constructible]
    public IceWyrmScale() : base(0x26B2)
    {
        LootType = LootType.Blessed;
        Hue = 2729;
        Weight = 20.0;
        QuestItem = true;
    }

    public override int LabelNumber => 1154224; // Ice Wyrm Scale

    public override int Lifespan => 3600;

    public override void OnDoubleClick(Mobile from)
    {
        base.OnDoubleClick(from);

        // *You run your hand across the scale, it is cold to the touch and smooth like glass. You decide
        // to take it to the Master Tinker*
        from.PublicOverheadMessage(MessageType.Regular, 0x3B2, 1154225);
    }
}
