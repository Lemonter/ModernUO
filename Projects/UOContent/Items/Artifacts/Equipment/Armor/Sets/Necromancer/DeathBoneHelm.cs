using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/Sets/Necromancer/DeathBoneHelm.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class DeathBoneHelm : BoneHelm
{
    [Constructible]
    public DeathBoneHelm()
        : base()
    {
        this.Attributes.RegenHits = 1;
        this.Attributes.RegenMana = 1;

    }

    public override int LabelNumber => 1074305; // Death's Essence

    public override int BasePhysicalResistance => 4;

    public override int BaseFireResistance => 9;

    public override int BaseColdResistance => 3;

    public override int BasePoisonResistance => 6;

    public override int BaseEnergyResistance => 8;
}
