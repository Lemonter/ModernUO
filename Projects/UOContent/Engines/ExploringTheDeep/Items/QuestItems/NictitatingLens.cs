using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/ExploringTheDeep/Items/QuestItems/NictitatingLens.cs). The
///     human/elf and gargoyle variants of one of the four Salvager's Suit pieces, both made by Josef
///     Skimmons for the orc drilling machine schematics.
/// </summary>
[SerializationGenerator(0, false)]
public partial class NictitatingLens : ElvenGlasses
{
    [Constructible]
    public NictitatingLens()
    {
        Hue = 1916;
        Weight = 2.0;
        LootType = LootType.Blessed;
        QuestItem = true;
    }

    public override int LabelNumber => 1154234; // Nictitating Lens

    public override int BasePhysicalResistance => 2;
    public override int BaseFireResistance => 4;
    public override int BaseColdResistance => 4;
    public override int BasePoisonResistance => 3;
    public override int BaseEnergyResistance => 2;

    public override int InitMinHits => 50;
    public override int InitMaxHits => 60;

    public override void OnDoubleClick(Mobile from)
    {
        base.OnDoubleClick(from);

        // *Finely crafted lenses for use in allowing the wearer to maintain visual acuity while
        // navigating an aquatic environment*
        from.PublicOverheadMessage(MessageType.Regular, 0x3B2, 1154235);
    }
}

[SerializationGenerator(0, false)]
public partial class GargishNictitatingLens : GargishGlasses
{
    [Constructible]
    public GargishNictitatingLens()
    {
        Hue = 1916;
        Weight = 2.0;
        LootType = LootType.Blessed;
        QuestItem = true;
    }

    public override int LabelNumber => 1154234; // Nictitating Lens

    public override int BasePhysicalResistance => 2;
    public override int BaseFireResistance => 4;
    public override int BaseColdResistance => 4;
    public override int BasePoisonResistance => 3;
    public override int BaseEnergyResistance => 2;

    public override int InitMinHits => 50;
    public override int InitMaxHits => 60;

    public override void OnDoubleClick(Mobile from)
    {
        base.OnDoubleClick(from);

        // *Finely crafted lenses for use in allowing the wearer to maintain visual acuity while
        // navigating an aquatic environment*
        from.PublicOverheadMessage(MessageType.Regular, 0x3B2, 1154235);
    }
}
