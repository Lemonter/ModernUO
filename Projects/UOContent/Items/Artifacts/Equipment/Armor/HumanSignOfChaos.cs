using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/HumanSignOfChaos.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class HumanSignOfChaos : ChaosShield
{
    [Constructible]
    public HumanSignOfChaos()
        : base()
    {
        Hue = 2075;
        Attributes.AttackChance = 5;
        Attributes.DefendChance = 10;
        Attributes.CastSpeed = 1;
    }

    public override int LabelNumber => 1113535; // Sign of Chaos

    public override int BasePhysicalResistance => 3;

    public override int BaseFireResistance => 2;

    public override int BaseColdResistance => 2;

    public override int BasePoisonResistance => 2;

    public override int BaseEnergyResistance => 2;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;
}
