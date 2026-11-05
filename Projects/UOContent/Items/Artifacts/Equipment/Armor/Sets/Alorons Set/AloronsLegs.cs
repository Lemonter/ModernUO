using ModernUO.Serialization;
using System;
using Server.Mobiles;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/Sets/Alorons Set/AloronsLegs.cs).
///     AloronsLegs: TigerPeltLegs has no counterpart here; re-based on BaseArmor with its ServUO art/stat line (`0x7824`).
/// </summary>
[SerializationGenerator(0, false)]
public partial class AloronsLegs : BaseArmor
{
    public override ArmorMaterialType MaterialType => ArmorMaterialType.Leather;

    public override int ArmorBase => 13;

    public override int OldStrReq => 10;

    public override int AosStrReq => 25;

    public override int InitMaxHits => 40;

    public override int InitMinHits => 30;

    public override double DefaultWeight => 2.0;

    public override int LabelNumber => 1156243; // Aloron's Armor

    public override int BasePhysicalResistance => 7;

    public override int BaseFireResistance => 7;

    public override int BaseColdResistance => 6;

    public override int BasePoisonResistance => 7;

    public override int BaseEnergyResistance => 7;

    [Constructible]
    public AloronsLegs() : base(0x7824)
    {
        Attributes.BonusDex = 4;
        Attributes.BonusStam = 4;
        Attributes.RegenStam = 3;

    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1156345); // Dinosaur Slayer
    }
}
