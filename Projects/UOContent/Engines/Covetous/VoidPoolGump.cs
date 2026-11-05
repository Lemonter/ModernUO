using System.Collections.Generic;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.VoidPool;

/// <summary>
///     Ported from ServUO's Covetous void pool (Gumps.cs) — the scoreboard. ServUO's version also shows
///     stored best-single/overall boards and a "best wave" entry; those come from its points framework's
///     persisted leaderboard, which this codebase doesn't have, so this board shows the battle that is
///     running right now (the same table the controller scores into).
/// </summary>
public class VoidPoolGump : Gump
{
    private const int Red = 0x4800;
    private const int Orange = 0xB104;
    private const int PerPage = 19;

    public VoidPoolController Controller { get; }
    public PlayerMobile User { get; }

    public VoidPoolGump(VoidPoolController controller, PlayerMobile user, int page = 0) : base(50, 50)
    {
        Controller = controller;
        User = user;

        AddBackground(0, 0, 500, 620, 9350);
        AddPage(page);

        AddHtmlLocalized(10, 10, 200, 16, 1152531, Red, false, false); // The Void Pool
        AddHtmlLocalized(10, 30, 200, 16, controller.Map == Map.Felucca ? 1012001 : 1012000, Red, false, false); // Fel/Tram

        if (controller.OnGoing)
        {
            AddHtmlLocalized(10, 50, 200, 16, 1152914, Orange, false, false); // Current Battle:
            AddHtmlLocalized(180, 50, 200, 16, 1152915, controller.Wave.ToString(), Orange, false, false); // Wave ~1_WAVE~
        }
        else
        {
            AddHtmlLocalized(10, 50, 200, 16, 1152916, Orange, false, false); // Next Battle:
        }

        AddHtmlLocalized(10, 90, 100, 16, 1152541, Orange, false, false); // RANK
        AddHtmlLocalized(100, 90, 300, 16, 1152542, Orange, false, false); // PLAYER
        AddHtmlLocalized(400, 90, 100, 16, 1152543, Orange, false, false); // SCORE

        var scores = new List<KeyValuePair<Mobile, long>>(controller.CurrentScore);
        scores.Sort((a, b) => b.Value.CompareTo(a.Value));

        var rank = page * PerPage;
        var y = 130;

        for (var i = rank; i < scores.Count && i < rank + PerPage; i++)
        {
            AddHtml(10, y, 50, 16, $"{i + 1}", false, false);
            AddHtml(100, y, 300, 16, scores[i].Key.Name ?? "?", false, false);
            AddHtml(400, y, 100, 16, scores[i].Value.ToString(), false, false);

            y += 20;
        }

        if (page > 0)
        {
            AddHtmlLocalized(120, 570, 100, 16, 1044044, Orange, false, false); // PREV PAGE
            AddButton(80, 570, 4014, 4015, 1000 + page - 1);
        }

        if (rank + PerPage < scores.Count)
        {
            AddHtmlLocalized(320, 570, 100, 16, 1044045, Orange, false, false); // NEXT PAGE
            AddButton(404, 570, 4005, 4006, 1000 + page + 1);
        }
    }

    public override void OnResponse(NetState state, in RelayInfo info)
    {
        if (info.ButtonID >= 1000)
        {
            User.SendGump(new VoidPoolGump(Controller, User, info.ButtonID - 1000));
        }
    }
}

/// <summary>Ported from the void pool's "Best Wave" board — an in-world plaque that opens the scoreboard.
/// The board itself shows the running battle here (see the gump's note about the missing leaderboard).</summary>
[ModernUO.Serialization.SerializationGenerator(0, false)]
public partial class BestWaveBoard : Item
{
    public override bool ForceShowProperties => true;

    [Constructible]
    public BestWaveBoard() : base(7774)
    {
        Name = "Бассейн Пустоты — лучшая волна";
        Movable = false;
    }

    public override void OnDoubleClick(Mobile m)
    {
        if (m is PlayerMobile pm && m.InRange(Location, 3))
        {
            var controller = m.Map == Map.Felucca ? VoidPoolController.InstanceFel : VoidPoolController.InstanceTram;

            if (controller != null)
            {
                pm.SendGump(new VoidPoolGump(controller, pm));
            }
        }
    }
}
