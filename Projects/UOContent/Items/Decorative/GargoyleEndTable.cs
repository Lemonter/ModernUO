using ModernUO.Serialization;

namespace Server.Items;

[Furniture]
[SerializationGenerator(0, false)]
public partial class GargoyleEndTable : Item
{
    [Constructible]
    public GargoyleEndTable() : base(0x4041)
    {
    }

    public override double DefaultWeight => 1.0;
}
