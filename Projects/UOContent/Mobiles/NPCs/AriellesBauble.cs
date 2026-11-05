using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class AriellesBauble : Item
{
    [Constructible]
    public AriellesBauble() : base(0x23B)
    {
        Weight = 2.0;
        LootType = LootType.Blessed;
    }

    public override int LabelNumber => 1073137; // A bauble
}
