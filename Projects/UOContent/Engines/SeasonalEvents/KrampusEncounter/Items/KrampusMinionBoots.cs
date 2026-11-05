using ModernUO.Serialization;

namespace Server.Items;

/// <summary>Ported from ServUO (Scripts/Services/Seasonal Events/KrampusEncounter/Items/KrampusMinionBoots.cs).</summary>
[SerializationGenerator(0, false)]
public partial class KrampusMinionBoots : BaseShoes
{
    public override int LabelNumber => 1125637; // krampus minion boots

    [Constructible]
    public KrampusMinionBoots() : this(0)
    {
        Weight = 2.0;
    }

    [Constructible]
    public KrampusMinionBoots(int hue) : base(0xA28D, hue)
    {
    }
}
