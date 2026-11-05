using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/WardedDemonboneBracers.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class WardedDemonboneBracers : BoneArms
{
    public override int LabelNumber => 1115775; // Warded Demonbone Bracers

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    public override int PhysicalResistance => 6;

    public override int FireResistance => 8;

    public override int ColdResistance => 4;

    public override int PoisonResistance => 5;

    public override int EnergyResistance => 5;

    [Constructible]
    public WardedDemonboneBracers()
    {
        Hue = 0x2E2;
        Attributes.RegenMana = 1;
        Attributes.LowerManaCost = 6;
        Attributes.LowerRegCost = 12;
        ArmorAttributes.MageArmor = 1;
    }
}
