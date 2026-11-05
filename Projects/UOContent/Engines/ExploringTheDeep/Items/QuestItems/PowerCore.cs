using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/ExploringTheDeep/Items/QuestItems/PowerCore.cs). One of the
///     four spare parts locked inside the supply barrels around Gravewater Lake.
/// </summary>
[SerializationGenerator(0, false)]
public partial class PowerCore : BaseDecayingItem
{
    [Constructible]
    public PowerCore() : base(0x47E6)
    {
        Weight = 1.0;
        LootType = LootType.Blessed;
        Hue = 1967;
        QuestItem = true;
    }

    public override int LabelNumber => 1154429; // Power Core

    public override int Lifespan => 18000;
}
