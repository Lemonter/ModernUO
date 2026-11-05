using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/DefenderOfTheMagus.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class DefenderOfTheMagus : MetalShield
{
    [Constructible]
    public DefenderOfTheMagus()
    {
        Hue = 590;
        Attributes.SpellChanneling = 1;
        Attributes.DefendChance = 10;
        Attributes.CastRecovery = 1;
        //Random Resonance:
        switch (Utility.Random(5))
        {
            case 0:
                break;
            case 1:
                break;
            case 2:
                break;
            case 3:
                break;
            case 4:
                break;
        }
        //Random Resist:
        switch (Utility.Random(5))
        {
            case 0:
                ColdBonus = 10;
                break;
            case 1:
                FireBonus = 10;
                break;
            case 2:
                PhysicalBonus = 10;
                break;
            case 3:
                PoisonBonus = 10;
                break;
            case 4:
                EnergyBonus = 10;
                break;
        }

    }

    public override int LabelNumber => 1113851; // Defender of the Magus

    public override int BasePhysicalResistance => 0;

    public override int BaseFireResistance => 1;

    public override int BaseColdResistance => 0;

    public override int BasePoisonResistance => 0;

    public override int BaseEnergyResistance => 0;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;
}
