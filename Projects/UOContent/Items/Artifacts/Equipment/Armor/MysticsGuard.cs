using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/MysticsGuard.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class MysticsGuard : GargishWoodenShield
{
    public override int LabelNumber => 1113536;

    public override int ArtifactRarity => 5;

    [Constructible]
    public MysticsGuard()
        : base()
    {
        Attributes.SpellChanneling = 1;
        Attributes.DefendChance = 10;
        Attributes.CastRecovery = 2;
        Hue = 0x671;
    }

    public override int BasePhysicalResistance => 10;

    public override int BaseFireResistance => 0;

    public override int BaseColdResistance => 0;

    public override int BasePoisonResistance => 0;

    public override int BaseEnergyResistance => 1;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;
}
