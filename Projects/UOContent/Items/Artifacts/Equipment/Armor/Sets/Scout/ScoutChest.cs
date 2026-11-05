using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/Sets/Scout/ScoutChest.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class ScoutChest : StuddedChest
{
    public override int LabelNumber => 1080476; // Scout's Studded Tunic

    public override int BasePhysicalResistance => 7;

    public override int BaseFireResistance => 7;

    public override int BaseColdResistance => 7;

    public override int BasePoisonResistance => 7;

    public override int BaseEnergyResistance => 7;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    [Constructible]
    public ScoutChest() : base()
    {
        this.Hue = 1148;
        this.Weight = 8;

        this.Attributes.BonusDex = 1;
        this.ArmorAttributes.MageArmor = 1;

    }
}
