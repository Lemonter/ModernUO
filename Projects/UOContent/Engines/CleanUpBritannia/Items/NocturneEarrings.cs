using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class NocturneEarrings : SilverEarrings
{
    [Constructible]
    public NocturneEarrings()
    {
        Hue = 0x3E5;
        Attributes.NightSight = 1;
    }

    public override int LabelNumber => 1080189; // Nocturne Earrings
}
