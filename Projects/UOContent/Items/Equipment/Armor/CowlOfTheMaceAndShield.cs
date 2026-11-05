using ModernUO.Serialization;
using System;
using Server.Engines.Craft;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Armor/CowlOfTheMaceAndShield.cs).
///     Dropped (absent here): IsArtifact.
/// </summary>
[SerializationGenerator(0, false)]
public partial class CowlOfTheMaceAndShield : AssassinsCowl
{
    public override int LabelNumber => 1159228; // cowl of the mace & shield

    public override int BasePhysicalResistance => 10;
    public override int BaseFireResistance => 10;
    public override int BaseColdResistance => 10;
    public override int BasePoisonResistance => 10;
    public override int BaseEnergyResistance => 10;

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;

    [Constructible]
    public CowlOfTheMaceAndShield()
        : this(0)
    {
    }

    public CowlOfTheMaceAndShield(int hue)
        : base(hue)
    {
        Attributes.BonusStr = 10;
        Attributes.BonusDex = 5;
    }
}
