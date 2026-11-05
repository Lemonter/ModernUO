using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class RawRotwormMeat : CookableFood
{
    [Constructible]
    public RawRotwormMeat(int amount = 1) : base(0x2DB9, 10)
    {
        Stackable = true;
        Weight = 0.1;
        Amount = amount;
    }

    public override Food Cook() => null;
}
