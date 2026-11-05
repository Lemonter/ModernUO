using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/Sets/Acolyte/GreymistChest.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class GreymistChest : LeatherChest
{
    [Constructible]
    public GreymistChest()
        : base()
    {
        this.Attributes.BonusMana = 2;
        this.Attributes.SpellDamage = 2;

    }

    public override int LabelNumber => 1074307; // Greymist Armor

    public override int BasePhysicalResistance => 7;

    public override int BaseFireResistance => 7;

    public override int BaseColdResistance => 3;

    public override int BasePoisonResistance => 4;

    public override int BaseEnergyResistance => 4;
}
