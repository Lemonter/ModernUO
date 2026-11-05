using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/LordBlackthornsExemplar.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class LordBlackthornsExemplar : ChaosShield
{
    [Constructible]
    public LordBlackthornsExemplar()
        : base()
    {
        Hue = 0x501;
    }

    public override int LabelNumber => 1079793; // Lord Blackthorn's Exemplar

    public override int ArtifactRarity => 11;

    public override int BasePhysicalResistance => 6;

    public override int BaseFireResistance => 10;

    public override int BaseColdResistance => 10;

    public override int BasePoisonResistance => 15;

    public override int BaseEnergyResistance => 10;

    public override int InitMinHits => 150;

    public override int InitMaxHits => 150;
}
