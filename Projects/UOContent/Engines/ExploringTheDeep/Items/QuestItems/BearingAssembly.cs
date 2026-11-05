using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/ExploringTheDeep/Items/QuestItems/BearingAssembly.cs). One of
///     the four spare parts locked inside the supply barrels around Gravewater Lake; the winch assembly
///     eats it.
/// </summary>
[SerializationGenerator(0, false)]
public partial class BearingAssembly : BaseDecayingItem
{
    [Constructible]
    public BearingAssembly() : base(0xE74)
    {
        Weight = 1.0;
        LootType = LootType.Blessed;
        QuestItem = true;
    }

    public override int LabelNumber => 1154430; // Bearing Assembly

    public override int Lifespan => 18000;
}
