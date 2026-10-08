using ModernUO.Serialization;

namespace Server.Items;

/// <summary>Tyball's Shadow wears one; a tenth of them survive the kill. Ported from ServUO
/// (Scripts/Items/Artifacts/Equipment/Clothing/ShroudOfTheCondemned.cs).</summary>
[SerializationGenerator(0, false)]
public partial class ShroudOfTheCondemned : BaseOuterTorso
{
    [Constructible]
    public ShroudOfTheCondemned() : base(0x1F04, 0xD6)
    {
        Hue = 2075;

        Attributes.BonusHits = 3;
        Attributes.BonusInt = 5;
    }

    public override double DefaultWeight => 3.0;

    public override int LabelNumber => 1113703; // Shroud of the Condemned
}
