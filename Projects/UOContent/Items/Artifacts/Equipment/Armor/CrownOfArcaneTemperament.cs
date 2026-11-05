using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/CrownOfArcaneTemperament.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class CrownOfArcaneTemperament : Circlet
{
    public override int RequiredRaces => Race.AllowGargoylesOnly;

    public override int LabelNumber => 1113762; // Crown of Arcane Temperament

    [Constructible]
    public CrownOfArcaneTemperament()
    {
        Attributes.BonusMana = 8;
        Attributes.RegenMana = 3;
        Attributes.SpellDamage = 8;
        Attributes.LowerManaCost = 6;
        Hue = 2012;
    }

    public override int BasePhysicalResistance => 10;

    public override int BaseFireResistance => 14;

    public override int BaseColdResistance => 4;

    public override int BasePoisonResistance => 12;

    public override int BaseEnergyResistance => 7;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;
}
