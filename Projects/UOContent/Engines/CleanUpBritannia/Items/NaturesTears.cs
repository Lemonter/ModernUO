using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class NaturesTears : BaseInstrument
{
    [Constructible]
    public NaturesTears() : base(0xE9C, 0x38, 0x39)
    {
        Hue = 2075;
        Weight = 5;
        Slayer = SlayerName.Fey;

        UsesRemaining = 450;
    }

    public override int LabelNumber => 1154373; // Nature's Tears

    public override int InitMinUses => 450;

    public override int InitMaxUses => 450;
}
