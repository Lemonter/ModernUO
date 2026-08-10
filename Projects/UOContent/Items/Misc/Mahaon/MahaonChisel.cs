using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class MahaonChisel : Item
{
    [Constructible]
    public MahaonChisel() : base(0x10E7)
    {
        Weight = 2.0;
        Name = "долото";
    }
}
