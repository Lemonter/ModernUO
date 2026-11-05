using System;
using ModernUO.Serialization;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Items;

/// <summary>
///     Ported from ServUO's Wrong Revamped dungeon (Items/Artifacts/BlanketOfDarknessArtifact.cs) — the
///     artifact you unroll beside a secured camp to log out on the spot.
///
///     Adapted: <c>IsArtifact</c> is not a virtual on Item here, so it is dropped; <c>GetProperties</c>
///     takes an <see cref="IPropertyList" /> rather than an <c>ObjectPropertyList</c>; the gump is a
///     <see cref="StaticGump{T}" /> with its close timer held as a
///     <see cref="TimerExecutionToken" /> (the original keeps a raw <c>Timer</c> field), and
///     <see cref="PlayerMobile.BlanketOfDarknessLogout" /> does not exist, so the client is disconnected
///     with a reason instead, the same way the bedroll logs a player out.
/// </summary>
[SerializationGenerator(0, false)]
public partial class BlanketOfDarkness : Item
{
    public override int LabelNumber => 1152304; // Blanket Of Darkness

    public override double DefaultWeight => 10.0;

    [Constructible]
    public BlanketOfDarkness() : base(0xA57)
    {
        Hue = 2076;
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (Parent != null || !VerifyMove(from))
        {
            return;
        }

        if (!from.InRange(this, 2))
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
            return;
        }

        if (ItemID == 0xA57) // rolled
        {
            var dir = PlayerMobile.GetDirection4(from.Location, Location);

            if (dir is Direction.North or Direction.South)
            {
                ItemID = 0xA55;
            }
            else
            {
                ItemID = 0xA56;
            }
        }
        else // unrolled
        {
            ItemID = 0xA57;

            if (!from.HasGump<LogoutGump>())
            {
                var entry = Campfire.GetEntry(from);

                if (entry?.Safe == true)
                {
                    from.SendGump(new LogoutGump(entry, this));
                }
            }
        }
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1061078, 8); // artifact rarity ~1_val~
    }

    private class LogoutGump : StaticGump<LogoutGump>
    {
        private readonly BlanketOfDarkness _blanket;
        private readonly CampfireEntry _entry;

        private TimerExecutionToken _closeTimerToken;

        public LogoutGump(CampfireEntry entry, BlanketOfDarkness blanket) : base(100, 0)
        {
            _entry = entry;
            _blanket = blanket;

            Timer.StartTimer(TimeSpan.FromSeconds(10.0), CloseGump, out _closeTimerToken);
        }

        protected override void BuildLayout(ref StaticGumpBuilder builder)
        {
            builder.AddBackground(0, 0, 400, 350, 0xA28);

            builder.AddHtmlLocalized(100, 20, 200, 35, 1011015); // <center>Logging out via camping</center>

            /* Using a bedroll in the safety of a camp will log you out of the game safely.
             * If this is what you wish to do choose CONTINUE and you will be logged out.
             * Otherwise, select the CANCEL button to avoid logging out at this time.
             * The camp will remain secure for 10 seconds at which time this window will close
             * and you not be logged out.
             */
            builder.AddHtmlLocalized(50, 55, 300, 140, 1011016, true, true);

            builder.AddButton(45, 298, 0xFA5, 0xFA7, 1);
            builder.AddHtmlLocalized(80, 300, 110, 35, 1011011); // CONTINUE

            builder.AddButton(200, 298, 0xFA5, 0xFA7, 0);
            builder.AddHtmlLocalized(235, 300, 110, 35, 1011012); // CANCEL
        }

        public override void OnResponse(NetState sender, in RelayInfo info)
        {
            var pm = _entry.Player;

            _closeTimerToken.Cancel();

            if (Campfire.GetEntry(pm) != _entry)
            {
                return;
            }

            if (info.ButtonID == 1 && _entry.Safe && _blanket.Parent == null && _blanket.IsAccessibleTo(pm)
                && _blanket.VerifyMove(pm) && _blanket.Map == pm.Map && pm.InRange(_blanket, 2))
            {
                pm.PlaceInBackpack(_blanket);

                sender.Disconnect("Used a blanket of darkness to log out.");
            }

            Campfire.RemoveEntry(_entry);
        }

        private void CloseGump()
        {
            Campfire.RemoveEntry(_entry);
            _entry.Player.CloseGump<LogoutGump>();
        }
    }
}
