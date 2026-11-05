using ModernUO.Serialization;
using Server.Network;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfDoom/Items/AncientClayVase.cs).
///     The <c>Stealing.ItemStolen</c> hint hook is dropped: that event does not exist here. The vase-spawner
///     registration on deserialize is dropped with <c>VaseSpawner</c> (see the report).
/// </summary>
[SerializationGenerator(0, false)]
public partial class AncientClayVase : Item, ICarvable
{
    [SerializableField(0)]
    private bool _doomEvent;

    public override int LabelNumber => 1155625; // Ancient Clay Vase

    [Constructible]
    public AncientClayVase() : this(false)
    {
    }

    [Constructible]
    public AncientClayVase(bool doom) : base(0x42B3)
    {
        _doomEvent = doom;
        Hue = 2676;
        LootType = LootType.Blessed;
    }

    public void Carve(Mobile from, Item item)
    {
        if (!IsChildOf(from.Backpack))
        {
            return;
        }

        from.PlaySound(Utility.Random(0x3E, 4));
        // *The vase shatters as you cut away the sealing wax!*
        from.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 1155629, from.NetState);

        Delete();

        if (Utility.RandomDouble() < 0.5)
        {
            from.AddToBackpack(new AncientParchment());
        }
    }
}
