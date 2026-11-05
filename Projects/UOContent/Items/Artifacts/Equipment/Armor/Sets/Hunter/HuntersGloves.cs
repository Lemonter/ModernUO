using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/Sets/Hunter/HuntersGloves.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class HunterGloves : LeafGloves
{
    [Constructible]
    public HunterGloves()
        : base()
    {
        this.Attributes.RegenHits = 1;
        this.Attributes.Luck = 50;

    }

    public override int LabelNumber => 1074301; // Hunter's Garb

    public override int BasePhysicalResistance => 9;

    public override int BaseFireResistance => 6;

    public override int BaseColdResistance => 3;

    public override int BasePoisonResistance => 8;

    public override int BaseEnergyResistance => 4;
}
