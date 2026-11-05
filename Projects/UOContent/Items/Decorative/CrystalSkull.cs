using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0x9A1A, 0x9A1B)]
[SerializationGenerator(0, false)]
public partial class CrystalSkull : Item
{
    [Constructible]
    public CrystalSkull() : base(0x9A1A)
    {
    }

    public override int LabelNumber => 1123474; // Crystal Skull
}
