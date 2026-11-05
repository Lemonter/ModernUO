using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/Sets/Scout/ScoutFemaleChest.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class ScoutFemaleChest : FemaleStuddedChest
{
    public override int LabelNumber => 1080479; // Scout's Female Studded Armor

    public override int BasePhysicalResistance => 7;

    public override int BaseFireResistance => 7;

    public override int BaseColdResistance => 7;

    public override int BasePoisonResistance => 7;

    public override int BaseEnergyResistance => 7;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    [Constructible]
    public ScoutFemaleChest() : base()
    {
        this.Hue = 1148;
        this.Weight = 6;

        this.Attributes.BonusDex = 1;
        this.ArmorAttributes.MageArmor = 1;

    }
}
