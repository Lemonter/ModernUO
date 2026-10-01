using ModernUO.Serialization;
using Server.Gumps;

namespace Server.Items;

/// <summary>Ported from ServUO (Scripts/Services/Peerless/MasterKey.cs). The altar hands one
/// of these to each participant when the offering completes; double-clicking it is how a party
/// gets into the boss room.
///
/// First use summons the boss and asks the holder to confirm taking the party along; later
/// uses, once the boss is already up, just offer the room to whoever is nearby.</summary>
[SerializationGenerator(0, false)]
public partial class MasterKey : PeerlessKey
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private PeerlessAltar _altar;

    [Constructible]
    public MasterKey(int itemID = 0x1012) : base(itemID)
    {
        LootType = LootType.Blessed;
    }

    public override int LabelNumber => 1074348; // master key

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            return;
        }

        if (_altar == null)
        {
            return;
        }

        // Someone already in the fight can't ferry more people in with the same key.
        if (_altar.Fighters.Contains(from))
        {
            from.SendLocalizedMessage(1063296); // You may not use that teleporter at this time.
            return;
        }

        if (_altar.Peerless == null)
        {
            from.CloseGump<ConfirmPartyGump>();
            from.SendGump(new ConfirmPartyGump(this));
        }
        else
        {
            _altar.SendConfirmations(from);
        }
    }
}
