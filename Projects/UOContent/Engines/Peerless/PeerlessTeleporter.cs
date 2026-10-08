using ModernUO.Serialization;
using Server.Gumps;

namespace Server.Items;

/// <summary>Ported from ServUO (Scripts/Services/Peerless/PeerlessTeleporter.cs). The way out
/// of a peerless arena: stepping on it asks the living whether they meant to leave, and simply
/// pushes the dead out.</summary>
[SerializationGenerator(0, false)]
public partial class PeerlessTeleporter : Teleporter
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private PeerlessAltar _altar;

    [Constructible]
    public PeerlessTeleporter(PeerlessAltar altar = null) => _altar = altar;

    public override bool OnMoveOver(Mobile m)
    {
        if (m.Alive)
        {
            ConfirmExitGump.DisplayTo(m, _altar);
            return true;
        }

        if (_altar != null)
        {
            _altar.Exit(m);
            return false;
        }

        return true;
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();

        _altar = null;
    }
}
