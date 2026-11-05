using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/ProtectoroftheBattleMage.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class ProtectoroftheBattleMage : LeatherChest
{
    public override int LabelNumber => 1113761; // Protector of the Battle Mage

    [Constructible]
    public ProtectoroftheBattleMage()
        : base()
    {
        Hue = 1159;
        Attributes.LowerManaCost = 8;
        Attributes.RegenMana = 2;
        Attributes.LowerRegCost = 10;
        Attributes.SpellDamage = 5;
    }

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    public override int BasePhysicalResistance => 10;

    public override int BaseFireResistance => 16;

    public override int BaseColdResistance => 10;

    public override int BasePoisonResistance => 8;

    public override int BaseEnergyResistance => 8;
}
