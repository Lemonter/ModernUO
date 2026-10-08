using System;
using System.Collections.Generic;
using Server.Guilds;
using Server.Mobiles;

namespace Server.Engines.VvV;

// Ported from real OSI/ServUO content (Scripts/Services/ViceVsVirtue/GuildStats.cs). The
// obsolete `VvVGuildBattleStats` (ServUO's own comment: "no longer used, left in for
// serialization purposes") isn't ported — a brand-new system here has no legacy saves to read.
public class VvVGuildStats
{
    public Guild Guild { get; }
    public int Score { get; set; }
    public int Kills { get; set; }
    public int ReturnedSigils { get; set; }

    public VvVGuildStats(Guild g) => Guild = g;
}

public class BattleTeam : IComparable<BattleTeam>
{
    public Guild Guild { get; }
    public int Score { get; set; }
    public int Silver { get; set; }

    public int Kills { get; set; }
    public int Assists { get; set; }
    public int Deaths { get; set; }
    public int Stolen { get; set; }

    public int ReturnedSigils => ViceReturned + VirtueReturned;

    public int ViceReturned { get; set; }
    public int VirtueReturned { get; set; }

    public int Disarmed { get; set; }

    public List<VvVPlayerBattleStats> PlayerStats { get; }

    public int CompareTo(BattleTeam team) => team == null ? -1 : team.Score.CompareTo(Score);

    public BattleTeam(Guild g)
    {
        Guild = g;
        PlayerStats = new List<VvVPlayerBattleStats>();
    }
}

public class VvVPlayerBattleStats
{
    public PlayerMobile Player { get; }
    public double Points { get; set; }

    public int Kills { get; set; }
    public int Assists { get; set; }
    public int Deaths { get; set; }
    public int Stolen { get; set; }

    public int ReturnedSigils => ViceReturned + VirtueReturned;

    public int ViceReturned { get; set; }
    public int VirtueReturned { get; set; }

    public int Disarmed { get; set; }

    public VvVPlayerBattleStats(PlayerMobile pm) => Player = pm;
}
