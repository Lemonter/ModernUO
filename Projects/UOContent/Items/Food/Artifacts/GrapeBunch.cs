using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class GrapeBunch : Food
{
    public override int LabelNumber => 1022513; // grapes

    [Constructible]
    public GrapeBunch() : base(3354, 1)
    {
        Weight = 1.0;
        FillFactor = 1;
        Stackable = false;
    }
}
