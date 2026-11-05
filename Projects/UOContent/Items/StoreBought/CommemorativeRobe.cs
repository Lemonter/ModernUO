using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class CommemorativeRobe : BaseOuterTorso
{
    [Constructible]
    public CommemorativeRobe() : base(0x4B9D)
    {
        LootType = LootType.Blessed;
    }

    public override int LabelNumber => 1157009; // Commemorative Robe
}
