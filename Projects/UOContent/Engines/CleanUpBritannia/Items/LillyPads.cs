using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class LillyPads : Item
{
    [Constructible]
    public LillyPads() : base(0xDBE)
    {
        Weight = 1.0;
    }

    public override string DefaultName => "лилии";
}
