using ModernUO.Serialization;

namespace Server.Items;

[Furniture]
[SerializationGenerator(0, false)]
public partial class TerMurStyleTable : Item
{
    [Constructible]
    public TerMurStyleTable() : base(0x4041)
    {
    }

    public override int LabelNumber => 1095321; // Ter-Mur style table

    public override double DefaultWeight => 10.0;
}
