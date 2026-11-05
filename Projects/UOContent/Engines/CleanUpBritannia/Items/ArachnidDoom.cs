using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class ArachnidDoom : BaseInstrument
{
    [Constructible]
    public ArachnidDoom() : base(0xE9C, 0x38, 0x39)
    {
        Hue = 1944;
        Weight = 4;
        Slayer = SlayerName.ArachnidDoom;

        UsesRemaining = 450;
    }

    public override int LabelNumber => 1154724; // Arachnid Doom

    public override int InitMinUses => 450;

    public override int InitMaxUses => 450;
}
