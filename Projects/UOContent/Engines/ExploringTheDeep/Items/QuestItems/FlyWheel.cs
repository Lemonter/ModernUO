using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/ExploringTheDeep/Items/QuestItems/FlyWheel.cs). One of the
///     four spare parts locked inside the supply barrels around Gravewater Lake.
/// </summary>
[SerializationGenerator(0, false)]
public partial class FlyWheel : BaseDecayingItem
{
    [Constructible]
    public FlyWheel() : base(0x46FE)
    {
        Weight = 1.0;
        LootType = LootType.Blessed;
        Hue = 1901;
        QuestItem = true;
    }

    public override int LabelNumber => 1154427; // Flywheel

    public override int Lifespan => 18000;
}
