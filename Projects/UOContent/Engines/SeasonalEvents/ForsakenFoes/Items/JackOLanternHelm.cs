using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/ForsakenFoes/Items/JackOLanternHelm.cs).
///     ServUO's <c>IsArtifact</c> is dropped: it does not exist here.
/// </summary>
[SerializationGenerator(0, false)]
public partial class JackOLanternHelm : BaseArmor
{
    public override int LabelNumber => 1125986; // jack o' lantern helm

    [Constructible]
    public JackOLanternHelm() : base(0xA3EA)
    {
        Weight = 3.0;
        Layer = Layer.Helm;
        Light = LightType.Circle300;
    }

    public override int BasePhysicalResistance => 12;

    public override int BaseFireResistance => 14;

    public override int BaseColdResistance => 4;

    public override int BasePoisonResistance => 8;

    public override int BaseEnergyResistance => 10;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    public override int AosStrReq => 10;

    public override ArmorMaterialType MaterialType => ArmorMaterialType.Plate;
}
