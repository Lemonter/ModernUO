using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/Sets/Paladin/PaladinHelm.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class PaladinHelm : PlateHelm
{
    [Constructible]
    public PaladinHelm()
        : base()
    {
        this.Attributes.RegenHits = 1;
        this.Attributes.AttackChance = 5;

    }

    public override int LabelNumber => 1074303; // Plate of Honor

    public override int BasePhysicalResistance => 4;

    public override int BaseFireResistance => 9;

    public override int BaseColdResistance => 3;

    public override int BasePoisonResistance => 6;

    public override int BaseEnergyResistance => 8;
}
