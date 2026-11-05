using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/Sets/Grizzle/GrizzleTunic.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class GrizzleTunic : BoneChest
{
    [Constructible]
    public GrizzleTunic()
        : base()
    {
        this.ArmorAttributes.MageArmor = 1;
        this.Attributes.BonusHits = 5;
        this.Attributes.NightSight = 1;

    }

    public override int LabelNumber => 1074467; // Tunic of the Grizzle

    public override int BasePhysicalResistance => 6;

    public override int BaseFireResistance => 10;

    public override int BaseColdResistance => 5;

    public override int BasePoisonResistance => 7;

    public override int BaseEnergyResistance => 10;
}
