using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class MahaonCoal : Item
{
    [Constructible]
    public MahaonCoal(int amount = 1) : base(0x19B8)
    {
        Stackable = true;
        Amount = amount;
        Hue = 0x966;
        Name = "уголь";
    }
}
