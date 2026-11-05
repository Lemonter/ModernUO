using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Lodestone : Item, ICommodity
{
    [Constructible]
    public Lodestone(int amount = 1) : base(0x5739)
    {
        Stackable = true;
        Amount = amount;
    }

    public override int LabelNumber => 1113348; // lodestone

    public int DescriptionNumber => LabelNumber;

    public bool IsDeedable => true;
}
