using ModernUO.Serialization;

namespace Server.Items;

/// <summary>Ported from ServUO (Scripts/Items/Quest/SerpentFangKey.cs) — dropped by
/// SerpentsFangHighExecutioner, one of the three clan keys the Citadel altar takes. Moved onto
/// the real PeerlessKey base now that it exists; see DragonFlameKey.cs.</summary>
[SerializationGenerator(0, false)]
public partial class SerpentFangKey : PeerlessKey
{
    [Constructible]
    public SerpentFangKey() : base(0x2002)
    {
        Weight = 2.0;
        Hue = 53;
        LootType = LootType.Blessed;
    }

    public override int LabelNumber => 1074341; // serpent fang key
}
