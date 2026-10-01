using System;
using System.Collections.Generic;
using System.Globalization;
using Server.Guilds;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.VvV;

// Ported from real OSI/ServUO content (Scripts/Services/ViceVsVirtue/Gumps/VvVBattleStatusGump.cs
// and BattleStatsGump.cs). ServUO's version subclasses its own convenience `BaseGump` wrapper
// (AddPage/User/GetTypeID()/Refresh helpers) — a completely different, byte-buffer-based class
// with the same name already exists in this codebase (`Server.Gumps.BaseGump`), so this is
// rewritten against the plain `Gump` (legacy) class instead, with an explicit `Refresh()` that
// just resends the gump.
public class VvVBattleStatusGump : Gump
{
    public PlayerMobile User { get; }
    public VvVBattle Battle { get; }

    public VvVBattleStatusGump(PlayerMobile pm, VvVBattle battle) : base(50, 50)
    {
        User = pm;
        Battle = battle;

        AddImage(0, 0, 30566);

        if (Core.Now >= Battle.NextSigilSpawn && Battle.Sigil is { Deleted: false })
        {
            AddImage(200, 300, 30583);
        }

        var teams = new List<BattleTeam>(Battle.Teams);
        teams.Sort();

        var offset = 216 / VvVBattle.ScoreToWin;

        for (var i = 0; i < teams.Count; i++)
        {
            var team = teams[i];

            if (team.Guild != null)
            {
                AddHtml(87, 115 + 31 * i, 50, 20, $"<basefont color=#FFFFFF>{team.Guild.Abbreviation}", false, false);
            }

            AddBackground(145, 120 + 31 * i, (int)Math.Min(216, team.Score * offset), 12, 30584);

            if (i == 2) // stupid gump only allows 3 to be shown
            {
                break;
            }
        }

        var count = Battle.Messages.Count - 1;
        var y = 206;

        for (var i = count; i >= 0; i--)
        {
            if (i <= count - 3)
            {
                break;
            }

            AddHtml(98, y, 250, 16, $"<basefont color=#80BFFF>{Battle.Messages[i]}", false, false);
            y += 16;
        }

        if (User.Guild is Guild gu)
        {
            var t = Battle.GetTeam(gu);

            AddHtml(87, 268, 50, 20, $"<basefont color=#FFFFFF>{gu.Abbreviation}", false, false);
            AddBackground(145, 271, (int)Math.Min(216, t.Score * offset), 12, 30584);
        }

        var left = Battle.StartTime + TimeSpan.FromMinutes(VvVBattle.Duration) - Core.Now;
        AddHtml(210, 21, 100, 20, $"<basefont color=#FF0000>{left:mm\\:ss}", false, false);
    }

    public void Refresh(bool sendGump, bool unused = false)
    {
        if (User.NetState == null)
        {
            return;
        }

        User.GetGumps().Close<VvVBattleStatusGump>();

        if (sendGump)
        {
            User.SendGump(new VvVBattleStatusGump(User, Battle));
        }
    }
}

// Ported from real OSI/ServUO content (Scripts/Services/ViceVsVirtue/Gumps/BattleStatsGump.cs).
public class BattleStatsGump : Gump
{
    public VvVBattle Battle { get; }

    private static readonly int _color = GumpColor.Convert32To16(0xB22222);

    public BattleStatsGump(PlayerMobile pm, VvVBattle battle) : base(50, 50)
    {
        Battle = battle;
        var leader = battle.GetLeader();

        if (leader?.Guild == null || pm.Guild is not Guild myGuild)
        {
            return;
        }

        AddBackground(0, 0, 500, 500, 9380);

        AddHtmlLocalized(0, 40, 500, 20, 1154645, "#1154945", _color); // The Battle between Vice and Virtue has ended!
        AddHtml(40, 65, 420, 20, $"<basefont color=#B22222>{leader.Guild.Name} [{leader.Guild.Abbreviation}] has won the battle!", false, false);

        var y = 90;

        if (leader.Guild.Alliance != null)
        {
            AddHtml(40, y, 420, 20, $"<basefont color=#B22222>The {leader.Guild.Alliance.Name} Alliance has won the battle!", false, false);
            y += 25;
        }

        var team = Battle.GetTeam(myGuild);
        var culture = CultureInfo.GetCultureInfo("en-US");

        AddHtmlLocalized(40, y, 420, 20, 1154947, team.Silver.ToString("N0", culture), _color); // Total Silver Points: ~1_val~
        y += 25;
        AddHtmlLocalized(40, y, 420, 20, 1154948, team.Score.ToString("N0", culture), _color); // Total Score: ~1_val~
        y += 25;
        AddHtmlLocalized(40, y, 420, 20, 1154949, team.Kills.ToString("N0", culture), _color);
        y += 25;
        AddHtmlLocalized(40, y, 420, 20, 1154950, team.Assists.ToString("N0", culture), _color);
        y += 25;
        AddHtmlLocalized(40, y, 420, 20, 1154951, team.Deaths.ToString("N0", culture), _color);
        y += 25;
        AddHtmlLocalized(40, y, 420, 20, 1154952, team.Stolen.ToString("N0", culture), _color);
        y += 25;
        AddHtmlLocalized(40, y, 420, 20, 1154953, team.ReturnedSigils.ToString("N0", culture), _color);
        y += 25;
        AddHtmlLocalized(40, y, 420, 20, 1154954, team.ViceReturned.ToString("N0", culture), _color);
        y += 25;
        AddHtmlLocalized(40, y, 420, 20, 1154955, team.VirtueReturned.ToString("N0", culture), _color);
        y += 25;
        AddHtmlLocalized(40, y, 420, 20, 1154956, team.Disarmed.ToString("N0", culture), _color);
    }
}
