using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class NestWithEggs : Item
{
    [Constructible]
    public NestWithEggs() : base(0x1AD4)
    {
        Hue = 2415;
        Weight = 2;
    }

    public override int LabelNumber => 1026868; // nest with eggs
}
