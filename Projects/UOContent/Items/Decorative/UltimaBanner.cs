using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0x42C9, 0x42CA)]
[SerializationGenerator(0, false)]
public partial class UltimaBanner : Item
{
    [Constructible]
    public UltimaBanner() : base(0x42C9)
    {
    }

    public override double DefaultWeight => 2.0;
}
