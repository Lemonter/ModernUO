using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class SosariaSap : Item
{
    [Constructible]
    public SosariaSap() : base(0x1848)
    {
        Weight = 1.0;
        LootType = LootType.Blessed;
    }

    public override int LabelNumber => 1074178; // Sap of Sosaria
}
