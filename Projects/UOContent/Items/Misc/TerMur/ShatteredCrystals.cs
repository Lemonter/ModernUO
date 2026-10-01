using ModernUO.Serialization;

namespace Server.Items;

/// <summary>Ported from ServUO (Scripts/Items/Quest/ShatteredCrystals.cs). The sixth of the
/// Prism of Light's crystal offerings.
///
/// This was originally written as a plain Item because the PeerlessKey base hadn't been
/// ported yet; it now sits on the real base along with the other five crystals in
/// Engines/Peerless/PrismOfLight/PrismOfLightKeys.cs.</summary>
[SerializationGenerator(0, false)]
public partial class ShatteredCrystals : PeerlessKey
{
    [Constructible]
    public ShatteredCrystals() : base(0x223F)
    {
        Weight = 1.0;
        Hue = 0x47E;
    }

    public override int LabelNumber => 1074266; // shattered crystal
}
