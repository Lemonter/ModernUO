using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class LillyPad : Item
{
    [Constructible]
    public LillyPad() : base(0xDBC)
    {
        Weight = 1.0;
    }

    public override string DefaultName => "лилия";
}
