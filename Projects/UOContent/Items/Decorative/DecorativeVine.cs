using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecorativeVines : Item
{
    [Constructible]
    public DecorativeVines() : this(Utility.Random(4))
    {
    }

    [Constructible]
    public DecorativeVines(int v) : base(0x2CF9)
    {
        if (v < 0 || v > 3)
        {
            v = 0;
        }

        ItemID += v;
        Weight = 1.0;
    }

    public override bool ForceShowProperties => ObjectPropertyList.Enabled;
}
