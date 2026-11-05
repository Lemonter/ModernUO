using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0x4B9D, 0x4B9E)]
[SerializationGenerator(0, false)]
public partial class AnniversaryRobe : BaseOuterTorso
{
    public override int LabelNumber => 1153496; // 15th Anniversary Commemorative Robe

    [Constructible]
    public AnniversaryRobe() : this(0x455)
    {
    }

    public AnniversaryRobe(int hue) : base(0x4B9D, hue)
    {
        LootType = LootType.Blessed;
        Weight = 1.0;
    }
}
