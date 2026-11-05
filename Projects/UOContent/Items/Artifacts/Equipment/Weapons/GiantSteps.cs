using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Weapons/GiantSteps.cs).
///     GiantSteps: GargishStoneLegs has no counterpart here; re-based on GargishStoneLegsType1.
/// </summary>
[SerializationGenerator(0, false)]
public partial class GiantSteps : GargishStoneLegsType1
{
    public override int LabelNumber => 1113537; // Giant Steps

    [Constructible]
    public GiantSteps()
        : base()
    {
        Hue = 656;
        Attributes.BonusStr = 5;
        Attributes.BonusDex = 5;
        Attributes.BonusHits = 5;
        Attributes.RegenHits = 2;
        Attributes.WeaponDamage = 10;
    }

    public override int BasePhysicalResistance => 18;

    public override int BaseFireResistance => 16;

    public override int BaseColdResistance => 4;

    public override int BasePoisonResistance => 8;

    public override int BaseEnergyResistance => 12;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;
}
