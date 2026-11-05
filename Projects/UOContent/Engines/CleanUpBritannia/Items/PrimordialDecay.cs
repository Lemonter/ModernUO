using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class PrimordialDecay : BaseInstrument
{
    [Constructible]
    public PrimordialDecay() : base(0xE9C, 0x38, 0x39)
    {
        Hue = 1927;
        Weight = 4;
        Slayer = SlayerName.ElementalBan;

        UsesRemaining = 450;
    }

    public override int LabelNumber => 1154723; // Primordial Decay

    public override int InitMinUses => 450;

    public override int InitMaxUses => 450;
}
