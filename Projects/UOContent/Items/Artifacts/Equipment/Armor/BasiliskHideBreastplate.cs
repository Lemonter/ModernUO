using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/BasiliskHideBreastplate.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class BasiliskHideBreastplate : DragonChest
{
    public override int LabelNumber => 1115444; // Basilisk Hide Breastplate

    [Constructible]
    public BasiliskHideBreastplate()
    {
        Resource = CraftResource.None;
        Hue = 1366;
        Attributes.BonusDex = 5;
        Attributes.RegenHits = 2;
        Attributes.RegenStam = 2;
        Attributes.RegenMana = 1;
        Attributes.DefendChance = 5;
        Attributes.LowerManaCost = 5;
    }

    public override int BasePhysicalResistance => 12;

    public override int BaseFireResistance => 14;

    public override int BaseColdResistance => 6;

    public override int BasePoisonResistance => 11;

    public override int BaseEnergyResistance => 5;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;
}
