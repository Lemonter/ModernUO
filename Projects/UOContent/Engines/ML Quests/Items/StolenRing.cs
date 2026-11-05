using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class StolenRing : SilverRing
{
    public override int LabelNumber => 1073124; // A ring engraved: "Beloved Ciala"

    [Constructible]
    public StolenRing()
    {
        LootType = LootType.Blessed;
        Weight = 1;
    }
}
