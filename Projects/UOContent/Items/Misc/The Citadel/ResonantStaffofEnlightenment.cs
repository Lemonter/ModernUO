using ModernUO.Serialization;

namespace Server.Items;

/// <summary>Ported from ServUO (Scripts/Items/Artifacts/Equipment/Weapons/
/// ResonantStaffOfEnlightenment.cs) — one of FireDaemonRenowned's two possible artifact
/// drops (see BaseRenowned.UniqueSAList). AbsorptionAttributes (a per-element "resonance"
/// bonus) dropped — BaseWeapon in this codebase has no such attribute container. IsArtifact
/// dropped too — see DespiseArtifacts.cs's class doc comment for why.</summary>
[SerializationGenerator(0, false)]
public partial class ResonantStaffofEnlightenment : QuarterStaff
{
    public override int LabelNumber => 1113757; // Resonant Staff of Enlightenment

    [Constructible]
    public ResonantStaffofEnlightenment()
    {
        Hue = 2401;
        WeaponAttributes.HitMagicArrow = 40;
        WeaponAttributes.MageWeapon = 20;
        Attributes.SpellChanneling = 1;
        Attributes.DefendChance = 10;
        Attributes.WeaponSpeed = 20;
        Attributes.WeaponDamage = -40;
        Attributes.LowerManaCost = 5;
        AosElementDamages.Cold = 100;
        Attributes.BonusInt = 5;
    }

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;
}
