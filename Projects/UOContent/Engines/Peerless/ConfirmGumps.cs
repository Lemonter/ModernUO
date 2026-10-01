using System;
using Server.Items;
using Server.Network;

namespace Server.Gumps;

/// <summary>Ported from ServUO (Scripts/Services/Peerless/ConfirmGumps.cs). Asked of the key
/// holder before a peerless key drags their whole party somewhere they can't see.</summary>
public class ConfirmPartyGump : Gump
{
    private readonly MasterKey _key;

    public ConfirmPartyGump(MasterKey key) : base(50, 50)
    {
        _key = key;

        Closable = false;
        Disposable = false;

        AddBackground(0, 0, 245, 145, 9250);
        AddHtmlLocalized(37, 20, 200, 60, 1072525, 0xFFFFFF, false, false);
        // Are you sure you wish to teleport your party to an unknown area?

        AddButton(157, 95, 4005, 4007, 1);
        AddHtmlLocalized(192, 96, 50, 20, 1046362, 0xFFFFFF, false, false); // Yes

        AddButton(40, 95, 4005, 4007, 0);
        AddHtmlLocalized(75, 96, 50, 20, 1046363, 0xFFFFFF, false, false); // No
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (info.ButtonID != 1 || _key?.Deleted != false)
        {
            return;
        }

        _key.Altar?.SendConfirmations(sender.Mobile);
        _key.Delete();
    }
}

/// <summary>Ported from ServUO. Offered to each party member in range — the boss room is
/// opt-in, and it withdraws itself after a minute so a stale gump can't drop someone into a
/// fight that has since moved on.</summary>
public class ConfirmEntranceGump : Gump
{
    private readonly PeerlessAltar _altar;
    private TimerExecutionToken _closeToken;

    public ConfirmEntranceGump(PeerlessAltar altar) : base(50, 50)
    {
        _altar = altar;

        Closable = false;
        Disposable = false;

        AddBackground(0, 0, 245, 145, 9250);
        AddHtmlLocalized(37, 20, 200, 60, 1072526, 0xFFFFFF, false, false);
        // Your party is teleporting to an unknown area. Do you wish to go?

        AddButton(157, 95, 4005, 4007, 1);
        AddHtmlLocalized(192, 96, 50, 20, 1046362, 0xFFFFFF, false, false); // Yes

        AddButton(40, 95, 4005, 4007, 0);
        AddHtmlLocalized(75, 96, 50, 20, 1046363, 0xFFFFFF, false, false); // No
    }

    public override void OnServerClose(NetState owner)
    {
        base.OnServerClose(owner);

        _closeToken.Cancel();
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        _closeToken.Cancel();

        if (info.ButtonID == 1)
        {
            _altar?.Enter(sender.Mobile);
        }
    }

    public static void DisplayTo(Mobile m, PeerlessAltar altar)
    {
        if (m?.NetState == null || altar == null)
        {
            return;
        }

        m.CloseGump<ConfirmEntranceGump>();

        var gump = new ConfirmEntranceGump(altar);
        m.SendGump(gump);

        Timer.StartTimer(TimeSpan.FromSeconds(60), () => m.CloseGump<ConfirmEntranceGump>(), out gump._closeToken);
    }
}

/// <summary>Ported from ServUO. Shown when someone walks onto a PeerlessTeleporter — leaving
/// mid-fight is a decision, not an accident.</summary>
public class ConfirmExitGump : Gump
{
    private readonly PeerlessAltar _altar;

    private ConfirmExitGump(PeerlessAltar altar) : base(50, 50)
    {
        _altar = altar;

        AddBackground(0, 0, 245, 145, 9250);
        AddHtmlLocalized(37, 20, 200, 60, 1075026, 0xFFFFFF, false, false); // Are you sure you wish to teleport?

        AddButton(157, 95, 4005, 4007, 1);
        AddHtmlLocalized(192, 96, 50, 20, 1046362, 0xFFFFFF, false, false); // Yes

        AddButton(40, 95, 4005, 4007, 0);
        AddHtmlLocalized(75, 96, 50, 20, 1046363, 0xFFFFFF, false, false); // No
    }

    public static void DisplayTo(Mobile m, PeerlessAltar altar)
    {
        if (m?.NetState == null || altar == null)
        {
            return;
        }

        m.CloseGump<ConfirmExitGump>();
        m.SendGump(new ConfirmExitGump(altar));
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (info.ButtonID == 1)
        {
            _altar?.Exit(sender.Mobile);
        }
    }
}
