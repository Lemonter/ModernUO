using ModernUO.Serialization;

namespace Server.Items;

/// <summary>Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfKhaldun/Items/CounterfeitPlatinum.cs).</summary>
[SerializationGenerator(0, false)]
public partial class CounterfeitPlatinum : Item
{
    public override int LabelNumber => 1158686; // counterfeit platinum

    [Constructible]
    public CounterfeitPlatinum() : base(0x1BF9)
    {
        Hue = 2500;
    }
}
