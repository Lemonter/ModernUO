using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/SpinedBloodwormBracers.cs).
///     SpinedBloodwormBracers: GargishClothArms has no counterpart here; re-based on GargishClothArmsType1.
/// </summary>
[SerializationGenerator(0, false)]
public partial class SpinedBloodwormBracers : GargishClothArmsType1
{
    public override int LabelNumber => 1113865; // Spined Bloodworm Bracers

    [Constructible]
    public SpinedBloodwormBracers()
    {
        Hue = 1642;
        Attributes.RegenHits = 2;
        Attributes.RegenStam = 2;
        Attributes.WeaponDamage = 10;
        Attributes.ReflectPhysical = 30;
    }

    public override int BasePhysicalResistance => 11;

    public override int BaseFireResistance => 10;

    public override int BaseColdResistance => 15;

    public override int BasePoisonResistance => 15;

    public override int BaseEnergyResistance => 10;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;
}
