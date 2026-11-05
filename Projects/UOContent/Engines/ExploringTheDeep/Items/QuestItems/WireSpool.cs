using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/ExploringTheDeep/Items/QuestItems/WireSpool.cs). One of the
///     four spare parts locked inside the supply barrels around Gravewater Lake.
/// </summary>
[SerializationGenerator(0, false)]
public partial class WireSpool : BaseDecayingItem
{
    [Constructible]
    public WireSpool() : base(0x4CDB)
    {
        Weight = 1.0;
        LootType = LootType.Blessed;
        Hue = 2315;
        QuestItem = true;
    }

    public override int LabelNumber => 1154428; // Wire Spool

    public override int Lifespan => 18000;
}
