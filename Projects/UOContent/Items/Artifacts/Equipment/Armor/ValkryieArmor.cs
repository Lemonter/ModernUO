using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/ValkryieArmor.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class ValkyrieArmor : LeatherBustierArms
{
    public override int LabelNumber => 1149957;

    public override int BasePhysicalResistance => 11;

    public override int BaseFireResistance => 14;

    public override int BaseColdResistance => 8;

    public override int BasePoisonResistance => 11;

    public override int BaseEnergyResistance => 8;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    [Constructible]
    public ValkyrieArmor()
    {
        Hue = 2112;
        Attributes.BonusStr = 5;
        Attributes.BonusDex = 5;
        Attributes.BonusInt = 5;
        Attributes.BonusStam = 5;
        Attributes.RegenStam = 3;
        Attributes.LowerManaCost = 10;
    }
}
