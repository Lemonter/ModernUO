using ModernUO.Serialization;
using Server.Network;

namespace Server.Items;

/// <summary>Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfDoom/Items/AncientParchment.cs).</summary>
[SerializationGenerator(0, false)]
public partial class AncientParchment : Item
{
    public override int LabelNumber => 1155627; // Ancient Parchment

    [Constructible]
    public AncientParchment() : this(1)
    {
    }

    [Constructible]
    public AncientParchment(int amount) : base(0x2269)
    {
        LootType = LootType.Blessed;
        Stackable = true;
        Amount = amount;
    }

    public override void OnDoubleClick(Mobile m)
    {
        if (IsChildOf(m.Backpack))
        {
            // *The parchment appears heavily worn and in need of restoration by a skilled Scribe...*
            m.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 1155628, m.NetState);
        }
    }
}

