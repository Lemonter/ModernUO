using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/Sets/Scout/ScoutGloves.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class ScoutGloves : StuddedGloves
{
    public override int LabelNumber => 1080477; // Scout's Studded Gloves

    public override int BasePhysicalResistance => 7;

    public override int BaseFireResistance => 7;

    public override int BaseColdResistance => 7;

    public override int BasePoisonResistance => 7;

    public override int BaseEnergyResistance => 7;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    [Constructible]
    public ScoutGloves() : base()
    {
        this.Hue = 1148;
        this.Weight = 1;

        this.Attributes.BonusDex = 1;
        this.ArmorAttributes.MageArmor = 1;

    }
}
