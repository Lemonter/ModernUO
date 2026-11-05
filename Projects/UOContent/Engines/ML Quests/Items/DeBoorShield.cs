using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DeBoorShield : MetalKiteShield
{
    public override int LabelNumber => 1075308; // Ancestral Shield

    [Constructible]
    public DeBoorShield() => LootType = LootType.Blessed;
}
