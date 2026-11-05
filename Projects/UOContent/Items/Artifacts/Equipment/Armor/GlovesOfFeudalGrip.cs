using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/GlovesOfFeudalGrip.cs).
///     GargishKiltOfFeudalVise: GargishPlateKilt has no counterpart here; re-based on BaseArmor with its ServUO art/stat line (`0x30C`).
/// </summary>
[SerializationGenerator(0, false)]
public partial class GlovesOfFeudalGrip : DragonGloves
{
    public override int LabelNumber => 1157349; // gloves of feudal grip

    [Constructible]
    public GlovesOfFeudalGrip()
    {
        Resource = CraftResource.None;

        Attributes.BonusStr = 8;
        Attributes.BonusStam = 8;
        Attributes.RegenHits = 3;
        Attributes.RegenMana = 3;
        Attributes.WeaponDamage = 30;
    }

    public override int BasePhysicalResistance => 15;

    public override int BaseFireResistance => 15;

    public override int BaseColdResistance => 15;

    public override int BasePoisonResistance => 15;

    public override int BaseEnergyResistance => 15;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    public override CraftResource DefaultResource => CraftResource.None;
}

[SerializationGenerator(0, false)]
public partial class GargishKiltOfFeudalVise : BaseArmor
{
    public override ArmorMaterialType MaterialType => ArmorMaterialType.Plate;

    public override int ArmorBase => 32;

    public override int OldStrReq => 80;

    public override int AosStrReq => 80;

    public override int RequiredRaces => Race.AllowGargoylesOnly;

    public override double DefaultWeight => 5.0;

    public override int LabelNumber => 1157367; // Kilt of Feudal Vise

    [Constructible]
    public GargishKiltOfFeudalVise() : base(0x30C)
    {
        Resource = CraftResource.None;

        Attributes.BonusStr = 8;
        Attributes.BonusStam = 8;
        Attributes.RegenHits = 3;
        Attributes.RegenMana = 3;
        Attributes.WeaponDamage = 30;
    }

    public override int BasePhysicalResistance => 15;

    public override int BaseFireResistance => 15;

    public override int BaseColdResistance => 15;

    public override int BasePoisonResistance => 15;

    public override int BaseEnergyResistance => 15;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    public override CraftResource DefaultResource => CraftResource.None;
}
