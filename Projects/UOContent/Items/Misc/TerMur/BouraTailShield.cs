using ModernUO.Serialization;

namespace Server.Items;

/// <summary>Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/BouraTailShield.cs).
/// IsArtifact dropped (no such virtual property on any Item base here). ArmorAttributes
/// doesn't have a ReactiveParalyze member locally — dropped, kept ReflectPhysical.</summary>
[SerializationGenerator(0, false)]
public partial class BouraTailShield : WoodenKiteShield
{
    [Constructible]
    public BouraTailShield()
    {
        Hue = 554;
        Attributes.ReflectPhysical = 10;
    }

    public override int LabelNumber => 1112361; // boura tail shield

    public override int BasePhysicalResistance => 8;
    public override int BaseFireResistance => 0;
    public override int BaseColdResistance => 0;
    public override int BasePoisonResistance => 0;
    public override int BaseEnergyResistance => 1;
    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;
}
