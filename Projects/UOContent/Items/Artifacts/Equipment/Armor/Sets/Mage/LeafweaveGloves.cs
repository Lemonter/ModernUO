using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/Sets/Mage/LeafweaveGloves.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class LeafweaveGloves : HideGloves
{
    [Constructible]
    public LeafweaveGloves()
        : base()
    {
        this.Attributes.RegenMana = 1;
        this.ArmorAttributes.MageArmor = 1;

    }

    public override int LabelNumber => 1074299; // Elven Leafweave

    public override int BasePhysicalResistance => 4;

    public override int BaseFireResistance => 9;

    public override int BaseColdResistance => 3;

    public override int BasePoisonResistance => 6;

    public override int BaseEnergyResistance => 8;
}
