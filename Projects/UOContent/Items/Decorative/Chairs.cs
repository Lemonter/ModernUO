using ModernUO.Serialization;

namespace Server.Items;

[Furniture]
[Flippable(0x4023, 0x4024)]
[SerializationGenerator(0, false)]
public partial class TerMurStyleChair : CraftableFurniture
{
    [Constructible]
    public TerMurStyleChair() : base(0x4023)
    {
    }

    public override int LabelNumber => 1095291; // Ter-Mur style chair

    public override double DefaultWeight => 20.0;
}
