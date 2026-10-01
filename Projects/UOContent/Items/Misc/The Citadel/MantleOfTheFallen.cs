using ModernUO.Serialization;

namespace Server.Items;

/// <summary>Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/
/// MantleOfTheFallen.cs) — one of FireDaemonRenowned's two possible artifact drops (see
/// BaseRenowned.UniqueSAList). Base is GargishClothChestType1 — this codebase split
/// ServUO's single GargishClothChest into Type1/Type2 variants (Type1 carries a
/// [TypeAlias("Server.Items.GargishClothChest")], confirming it's the same original
/// piece). IRepairable and SAAbsorptionAttributes dropped — neither exists in this
/// codebase for a clothing item.</summary>
[SerializationGenerator(0, false)]
public partial class MantleOfTheFallen : GargishClothChestType1
{
    public override int LabelNumber => 1113819; // Mantle of the Fallen
    public override int BasePhysicalResistance => 5;
    public override int BaseFireResistance => 8;
    public override int BaseColdResistance => 11;
    public override int BasePoisonResistance => 12;
    public override int BaseEnergyResistance => 8;
    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;

    [Constructible]
    public MantleOfTheFallen()
    {
        Hue = 1512;
        Attributes.LowerRegCost = 25;
        Attributes.BonusInt = 8;
        Attributes.BonusMana = 8;
        Attributes.RegenMana = 1;
        Attributes.SpellDamage = 5;
    }
}
