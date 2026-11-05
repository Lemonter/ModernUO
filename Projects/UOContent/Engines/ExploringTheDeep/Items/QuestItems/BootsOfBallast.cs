using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/ExploringTheDeep/Items/QuestItems/BootsOfBallast.cs). One of
///     the four pieces of the Salvager's Suit, made by Champ Huthwait for Mercutio's cutlass.
/// </summary>
[SerializationGenerator(0, false)]
public partial class BootsOfBallast : Boots
{
    [Constructible]
    public BootsOfBallast()
    {
        Hue = 2969;
        LootType = LootType.Blessed;
        StrRequirement = 10;
        QuestItem = true;
    }

    public override int LabelNumber => 1154242; // Boots of Ballast

    public override void OnDoubleClick(Mobile from)
    {
        base.OnDoubleClick(from);

        // *You struggle to lift the heavy boots for examination! You determine anyone venturing into the
        // sea wearing such a thing would quickly sink to the bottom*
        from.PublicOverheadMessage(MessageType.Regular, 0x3B2, 1154243);
    }
}
