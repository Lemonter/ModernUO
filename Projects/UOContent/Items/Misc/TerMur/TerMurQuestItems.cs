using ModernUO.Serialization;

namespace Server.Items;

/// <summary>Sliem's payment for "Unusual Goods": one virtue essence in a box. Ported from
/// ServUO (Scripts/Items/Containers/EssenceBox.cs).</summary>
[SerializationGenerator(0, false)]
public partial class EssenceBox : WoodenBox
{
    [Constructible]
    public EssenceBox()
    {
        Movable = true;
        Hue = 2306;

        DropItem(Loot.RandomEssence());
    }

    public override int LabelNumber => 1113770; // Essence Box
}

/// <summary>Master Cohenn's thesis, scattered among Bedlam's dead. Ported from ServUO
/// (Scripts/Items/Quest/DisintegratingThesisNotes.cs) — a decaying item like the peerless keys,
/// which is why it sits on the same base.</summary>
[SerializationGenerator(0, false)]
public partial class DisintegratingThesisNotes : PeerlessKey
{
    [Constructible]
    public DisintegratingThesisNotes() : base(0xE36)
    {
        Weight = 1.0;
        LootType = LootType.Blessed;
    }

    public override int LabelNumber => 1074440; // Disintegrating Thesis Notes

    /// <summary>A creature carrying one leaves it on its corpse rather than keeping it.</summary>
    public override DeathMoveResult OnInventoryDeath(Mobile parent) =>
        !parent.Player && !parent.IsDeadBondedPet ? DeathMoveResult.MoveToCorpse : base.OnInventoryDeath(parent);
}

/// <summary>Egwexem's sealed writ for Naxatillor. Ported from ServUO
/// (Scripts/Mobiles/NPCs/Egwexem.cs, where the item is declared next to the NPC).</summary>
[SerializationGenerator(0, false)]
public partial class EgwexemWrit : Item
{
    [Constructible]
    public EgwexemWrit() : base(0x0E34)
    {
    }

    public override int LabelNumber => 1112520; // Egwexem's Writ
}
