using ModernUO.Serialization;

namespace Server.Items;

/// <summary>Naxatillor's reward for "The Arisen". Ported from ServUO
/// (Scripts/Items/Artifacts/Equipment/Jewelry/NecklaceofDiligence.cs).</summary>
[SerializationGenerator(0, false)]
public partial class NecklaceofDiligence : SilverNecklace
{
    [Constructible]
    public NecklaceofDiligence()
    {
        Hue = 221;

        Attributes.RegenMana = 1;
        Attributes.BonusInt = 5;
    }

    public override int LabelNumber => 1113137; // Necklace of Diligence

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;
}
