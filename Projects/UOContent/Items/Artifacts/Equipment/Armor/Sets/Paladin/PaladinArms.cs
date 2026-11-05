using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/Sets/Paladin/PaladinArms.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class PaladinArms : PlateArms
{
    [Constructible]
    public PaladinArms()
        : base()
    {
        this.Attributes.RegenHits = 1;
        this.Attributes.AttackChance = 5;

    }

    public override int LabelNumber => 1074303; // Plate of Honor

    public override int BasePhysicalResistance => 8;

    public override int BaseFireResistance => 5;

    public override int BaseColdResistance => 5;

    public override int BasePoisonResistance => 7;

    public override int BaseEnergyResistance => 5;
}
