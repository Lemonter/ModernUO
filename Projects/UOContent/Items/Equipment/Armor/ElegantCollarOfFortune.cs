using ModernUO.Serialization;
using System;
using Server.Engines.Craft;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Armor/ElegantCollarOfFortune.cs).
///     Dropped (absent here): IsArtifact.
/// </summary>
[SerializationGenerator(0, false)]
public partial class ElegantCollarOfFortune : ElegantCollar
{
    public override int LabelNumber => 1159225; // elegant collar of fortune

    [Constructible]
    public ElegantCollarOfFortune()
        : base()
    {
        Attributes.Luck = 300;
        Attributes.RegenMana = 1;
    }

    public override int BasePhysicalResistance => 15;
    public override int BaseFireResistance => 10;
    public override int BaseColdResistance => 10;
    public override int BasePoisonResistance => 10;
    public override int BaseEnergyResistance => 15;

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;
}
