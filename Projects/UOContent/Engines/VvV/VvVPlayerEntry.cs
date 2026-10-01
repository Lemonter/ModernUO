using System;
using Server.Guilds;
using Server.Mobiles;

namespace Server.Engines.VvV;

// Ported from real OSI/ServUO content — was `PointsEntry` subclass there (see
// ViceVsVirtueSystem.cs header for why that framework doesn't exist here); plain class now.
public class VvVPlayerEntry
{
    public PlayerMobile Player { get; }
    public bool OneTimePointsRetention { get; set; }

    public int Points { get; set; }
    public int TotalKills { get; set; }
    public int TotalDeaths { get; set; }

    public EnemyKilledEntry KilledEntry { get; set; }

    public int Score { get; set; }
    public int Kills { get; set; }
    public int Deaths { get; set; }
    public int Assists { get; set; }
    public int ReturnedSigils { get; set; }
    public int DisarmedTraps { get; set; }
    public int StolenSigils { get; set; }

    public Guild Guild => Player?.Guild as Guild;

    private bool _active;

    public bool Active
    {
        get => _active;
        set
        {
            if (!_active && value)
            {
                if (OneTimePointsRetention)
                {
                    OneTimePointsRetention = false;
                }
                else
                {
                    Points = 0;
                }
            }

            _active = value;
        }
    }

    public DateTime ResignExpiration { get; set; }
    public bool Resigning => ResignExpiration > DateTime.MinValue;

    public VvVPlayerEntry(PlayerMobile pm)
    {
        Player = pm;
        _active = true;
        Points = ViceVsVirtueSystem.StartSilver;
    }

    public void AwardSilver(Mobile victim)
    {
        if (!ViceVsVirtueSystem.RestrictSilver(Player, victim))
        {
            Player.SendMessage($"You cannot earn silver from killing {victim.Name}!");
            return;
        }

        var entry = KilledEntry;

        if (entry == null)
        {
            KilledEntry = entry = new EnemyKilledEntry(victim);
        }
        else if (entry.Expired)
        {
            entry.TimesKilled = 1;
        }
        else
        {
            entry.TimesKilled++;
        }

        if (entry.TimesKilled > EnemyKilledEntry.MaxKillsForSilver)
        {
            Player.SendMessage($"You cannot earn any more silver from killing {victim.Name}.");
        }

        var silver = (int)(EnemyKilledEntry.KillSilver / (double)entry.TimesKilled);

        if (silver > 0)
        {
            // You have earned ~1_SILVER_AMOUNT~ pieces for vanquishing ~2_PLAYER_NAME~!
            Player.SendLocalizedMessage(1042736, $"{silver:N0} silver\t{victim.Name}");
            Points += silver;
        }
    }

    public class EnemyKilledEntry
    {
        public static int KillSilver = 20;
        public static int MaxKillsForSilver = 5;
        public static TimeSpan ExpireTime = TimeSpan.FromHours(3);

        public Mobile Killed { get; }
        public int TimesKilled { get; set; }
        public DateTime Expires { get; set; }

        public bool Expired => Expires < DateTime.UtcNow;

        public EnemyKilledEntry(Mobile killed)
        {
            Killed = killed;
            TimesKilled = 1;
            Expires = DateTime.UtcNow + ExpireTime;
        }
    }
}

public class TemporaryCombatant
{
    public static TimeSpan TempCombatTime = TimeSpan.FromMinutes(10);

    public Mobile From { get; }
    public Mobile Friendly { get; private set; }
    public DateTime StartTime { get; private set; }

    public Guild FriendlyGuild => Friendly?.Guild as Guild;

    public bool Expired => StartTime + TempCombatTime < DateTime.UtcNow;

    public TemporaryCombatant(Mobile from, Mobile friendlyTo)
    {
        From = from;
        Friendly = friendlyTo;
        StartTime = DateTime.UtcNow;
    }

    public void Reset() => StartTime = DateTime.UtcNow;
}
