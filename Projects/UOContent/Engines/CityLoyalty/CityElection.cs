/// <summary>
///     Ported from ServUO (Scripts/Services/City Loyalty System/Election.cs).
///
///     Substitutions:
///     <list type="bullet">
///         <item>
///             <c>ConfirmCallbackGump</c> does not exist here — replaced with
///             <see cref="CityConfirmGump" />, a thin <see cref="Server.Gumps.BaseConfirmGump" /> wrapper that runs a
///             callback on confirm.
///         </item>
///         <item>
///             <c>PlayerMobile.AddRewardTitle</c>/<c>RemoveRewardTitle</c> have no equivalent, so title bookkeeping moved
///             entirely into <see cref="CityLoyaltyEntry.Titles" />.
///         </item>
///         <item>Calendar math still uses <c>DateTime.Now</c> (election months are wall-clock by design).</item>
///     </list>
/// </summary>
using System;
using System.Collections.Generic;
using Server.Accounting;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.CityLoyalty;

[PropertyObject]
public class CityElection
{
    public static readonly int VotePeriod = 7;
    public static readonly int NominationDeadline = 24;

    private static readonly int[] _Periods = { 3, 6, 9, 12 };

    public CityElection(CityLoyaltySystem city)
    {
        City = city;

        ElectionEnded = true;
        Candidates = new List<BallotEntry>();

        GetDefaultStartTimes();
    }

    public List<BallotEntry> Candidates { get; set; }

    public DateTime[] StartTimes { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public CityLoyaltySystem City { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool ElectionEnded { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool Ongoing => CanNominate() || CanVote();

    [CommandProperty(AccessLevel.GameMaster)]
    public DateTime AutoPickGovernor { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public string Time1 => StartTimes.Length > 0 ? StartTimes[0].ToShortDateString() : "Empty";

    [CommandProperty(AccessLevel.GameMaster)]
    public string Time2 => StartTimes.Length > 1 ? StartTimes[1].ToShortDateString() : "Empty";

    [CommandProperty(AccessLevel.GameMaster)]
    public string Time3 => StartTimes.Length > 2 ? StartTimes[2].ToShortDateString() : "Empty";

    [CommandProperty(AccessLevel.GameMaster)]
    public string Time4 => StartTimes.Length > 3 ? StartTimes[3].ToShortDateString() : "Empty";

    [CommandProperty(AccessLevel.GameMaster)]
    public bool ForceStart
    {
        get => false;
        set
        {
            if (!value)
            {
                return;
            }

            if (Ongoing)
            {
                EndElection();
            }

            StartTimes = new DateTime[1];
            StartTimes[0] = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);
        }
    }

    public override string ToString() => $"{City?.City}";

    public bool TryNominate(PlayerMobile pm)
    {
        var pentry = City.GetPlayerEntry(pm);

        if (pm.Young)
        {
            pm.SendMessage("Молодые игроки не могут быть выдвинуты в бюллетень!");
            return false;
        }

        if (!City.IsCitizen(pm) || pentry == null)
        {
            // You must be a citizen of this City to nominate yourself for the ballot!
            pm.SendLocalizedMessage(1153890);
            return false;
        }

        if (City.GetLoyaltyRating(pm) < LoyaltyRating.Adored)
        {
            // You must at least be adored within the City to nominate yourself for the ballot.
            pm.SendLocalizedMessage(1153891);
            return false;
        }

        if (pm.Account is not Account a)
        {
            return false;
        }

        for (var i = 0; i < a.Length; i++)
        {
            var m = a[i];

            if (m is not PlayerMobile)
            {
                continue;
            }

            var ballot = Candidates.Find(entry => entry.Player == m);

            if (ballot != null)
            {
                pm.SendLocalizedMessage(ballot.Endorsements.Count > 0 ? 1153917 : 1153889);
                return false;
            }

            if (Candidates.Exists(entry => entry.Endorsements.Contains(m as PlayerMobile)))
            {
                // A character from this account has already endorsed a nominee!
                pm.SendLocalizedMessage(1153892);
                return false;
            }
        }

        Candidates.Add(new BallotEntry(pm, pentry.Love, pentry.Hate));
        // *You etch your name into the stone*
        pm.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 1153905, pm.NetState);
        // You have 24 hours to get your nomination endorsed...
        pm.SendLocalizedMessage(1154087);

        return true;
    }

    public bool TryEndorse(PlayerMobile pm, PlayerMobile nominee)
    {
        if (pm.Young)
        {
            pm.SendMessage("Молодые игроки не могут поддержать кандидата!");
            return false;
        }

        if (!City.IsCitizen(pm))
        {
            // You must be a citizen of this City to endorse a nominee for the ballot!
            pm.SendLocalizedMessage(1153893);
            return false;
        }

        if (pm.Account is not Account a)
        {
            return false;
        }

        for (var i = 0; i < a.Length; i++)
        {
            var m = a[i];

            if (m is not PlayerMobile)
            {
                continue;
            }

            if (Candidates.Exists(entry => entry.Endorsements.Contains(m as PlayerMobile)))
            {
                // A character from this account has already endorsed a nominee!
                pm.SendLocalizedMessage(1153892);
                return false;
            }

            if (Candidates.Exists(entry => entry.Player == m))
            {
                // A character from this account is currently nominated for candidacy and cannot offer an endorsement.
                pm.SendLocalizedMessage(1153912);
                return false;
            }
        }

        var pentry = Candidates.Find(e => e.Player == nominee);

        if (pentry == null)
        {
            return false;
        }

        // Are you sure you wish to endorse this nominee? All endorsements are final and cannot be changed!
        pm.SendGump(
            new CityConfirmGump(
                1154091,
                m =>
                {
                    pentry.Endorsements.Add(m as PlayerMobile);
                    // *You etch your endorsement for the nominee into the stone*
                    m.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 1153913, m.NetState);
                }
            )
        );

        return true;
    }

    public bool TryVote(PlayerMobile voter, PlayerMobile candidate)
    {
        if (!CanVote())
        {
            // The stone is not currently accepting votes.
            voter.SendLocalizedMessage(1153919);
            return false;
        }

        if (voter.Young)
        {
            voter.SendMessage("Молодые игроки не могут голосовать на выборах города!");
            return false;
        }

        if (!City.IsCitizen(voter))
        {
            // You must be a citizen of this City to vote!
            voter.SendLocalizedMessage(1153894);
            return false;
        }

        if (City.GetLoyaltyRating(voter) < LoyaltyRating.Respected)
        {
            // You must be at least respected within the city to vote.
            voter.SendLocalizedMessage(1154579);
            return false;
        }

        if (voter.Account is Account a)
        {
            for (var i = 0; i < a.Length; i++)
            {
                if (a[i] is PlayerMobile m && Candidates.Exists(e => e.Votes.Contains(m)))
                {
                    // This account has already cast a vote in this election. You may only vote once.
                    voter.SendLocalizedMessage(1153922);
                    return false;
                }
            }
        }

        var pentry = Candidates.Find(e => e.Player == candidate);

        if (pentry == null)
        {
            return false;
        }

        // Are you sure you wish to cast your vote for this candidate? All votes are final and cannot be changed!
        voter.SendGump(
            new CityConfirmGump(
                1153921,
                m =>
                {
                    pentry.Votes.Add(voter);
                    // *You etch your vote into the stone*
                    m.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 1153923, voter.NetState);
                }
            )
        );

        return true;
    }

    public void TryWithdraw(PlayerMobile pm)
    {
        var entry = Candidates.Find(e => e.Player == pm);

        if (entry == null)
        {
            // You are not currently on any ballot to withdraw from.
            pm.SendLocalizedMessage(1153924);
            return;
        }

        // Are you sure you wish to withdrawal?
        pm.SendGump(
            new CityConfirmGump(
                1153918,
                m =>
                {
                    if (!Candidates.Remove(entry))
                    {
                        return;
                    }

                    // *You smudge your name off the stone*
                    m.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 1153911, m.NetState);
                }
            )
        );
    }

    public double GetStanding(BallotEntry entry)
    {
        if (!Candidates.Contains(entry) || entry.Votes.Count <= 0)
        {
            return 0.0;
        }

        return entry.Votes.Count / (double)GetTotalVotes() * 100;
    }

    public int GetTotalVotes()
    {
        var votes = 0;

        foreach (var c in Candidates)
        {
            votes += c.Votes.Count;
        }

        return votes;
    }

    public void OnTick()
    {
        foreach (var dt in StartTimes)
        {
            if (dt.Year == DateTime.Now.Year && DateTime.Now.Month == dt.Month && DateTime.Now.Day > 14 && !ElectionEnded)
            {
                EndElection();
            }
            else if (dt.Year == DateTime.Now.Year && DateTime.Now.Month == dt.Month && DateTime.Now.Day == dt.Day &&
                     ElectionEnded)
            {
                StartNewElection();
            }
        }

        if (City.Governor == null && AutoPickGovernor != DateTime.MinValue && DateTime.Now > AutoPickGovernor)
        {
            if (City.GovernorElect != null)
            {
                var entry = Candidates.Find(c => c.Player == City.GovernorElect);

                if (entry != null)
                {
                    Candidates.Remove(entry);
                }

                City.GovernorElect = null;
            }

            if (Candidates.Count > 0)
            {
                Candidates.Sort();
                City.GovernorElect = Candidates[0].Player;
            }

            AutoPickGovernor = DateTime.MinValue;
        }

        if (CanNominate())
        {
            Candidates.RemoveAll(
                entry => entry.TimeOfNomination + TimeSpan.FromHours(NominationDeadline) < DateTime.Now &&
                         entry.Endorsements.Count == 0
            );
        }
    }

    public void StartNewElection()
    {
        ElectionEnded = false;
        Candidates.Clear();
        AutoPickGovernor = DateTime.MinValue;
    }

    public void GetDefaultStartTimes()
    {
        StartTimes = new DateTime[4];

        for (var i = 0; i < _Periods.Length; i++)
        {
            var dt = new DateTime(DateTime.Now.Year, _Periods[i], 1);

            if (dt < DateTime.Now)
            {
                dt = new DateTime(dt.Year + 1, dt.Month, dt.Day);
            }

            StartTimes[i] = dt;
        }
    }

    public static DateTime[] ValidateStartTimes(Mobile m, int[] times)
    {
        if (times == null || times.Length <= 0)
        {
            return null;
        }

        var startTimes = new DateTime[times.Length];

        for (var i = 0; i < times.Length; i++)
        {
            var month = times[i];

            if (month <= 0 || month > 12)
            {
                m.SendMessage($"Недопустимый месяц для #{i + 1}. Он должен быть от 1 до 12.");
                return null;
            }

            var dt = new DateTime(DateTime.Now.Year, month, 1);

            if (dt < DateTime.Now)
            {
                dt = new DateTime(dt.Year + 1, dt.Month, dt.Day);
            }

            startTimes[i] = dt;
        }

        if (!CheckConflict(startTimes))
        {
            return startTimes;
        }

        m.SendMessage("Даты слишком близко друг к другу. Попробуйте ещё раз.");
        return null;
    }

    public static bool CheckConflict(DateTime[] times)
    {
        if (times == null || times.Length == 0)
        {
            return false;
        }

        for (var i = 0; i < times.Length; i++)
        {
            var t = times[i];

            for (var j = 0; j < times.Length; j++)
            {
                if (i == j)
                {
                    continue;
                }

                if ((times[j] > t && times[j] - t < TimeSpan.FromDays(30)) ||
                    (times[j] < t && t - times[j] < TimeSpan.FromDays(30)))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public DateTime NextElection()
    {
        if (CanNominate() || CanVote())
        {
            return DateTime.MinValue;
        }

        var closest = DateTime.MinValue;

        foreach (var dt in StartTimes)
        {
            if (DateTime.Now < dt && (closest == DateTime.MinValue || dt - DateTime.Now < closest - DateTime.Now))
            {
                closest = dt;
            }
        }

        return closest;
    }

    public bool CanNominate() => CanNominate(out _);

    public bool CanNominate(out DateTime until)
    {
        until = DateTime.Now;

        foreach (var dt in StartTimes)
        {
            if (dt.Year == DateTime.Now.Year && DateTime.Now.Month == dt.Month &&
                DateTime.Now <= dt.AddDays(VotePeriod))
            {
                until = dt.AddDays(VotePeriod);
                return true;
            }
        }

        return false;
    }

    public bool CanVote() => CanVote(out _);

    public bool CanVote(out DateTime until)
    {
        until = DateTime.Now;

        foreach (var dt in StartTimes)
        {
            if (dt.Year == DateTime.Now.Year && DateTime.Now.Month == dt.Month &&
                DateTime.Now > dt.AddDays(VotePeriod) && DateTime.Now <= dt.AddDays(VotePeriod * 2))
            {
                until = dt.AddDays(VotePeriod * 2);
                return true;
            }
        }

        return false;
    }

    public void EndElection()
    {
        ElectionEnded = true;

        if (Candidates.Count > 0)
        {
            Candidates.Sort();
            City.GovernorElect = Candidates[0].Player;

            AutoPickGovernor = DateTime.Now + TimeSpan.FromDays(7);
        }

        if (City.GovernorElect == null)
        {
            City.PendingGovernor = true;
            AutoPickGovernor = DateTime.Now + TimeSpan.FromDays(Utility.RandomMinMax(2, 4));

            City.Stone?.InvalidateProperties();
        }
    }

    public void OnRejectOffice(PlayerMobile pm)
    {
        var entry = Candidates.Find(c => c.Player == pm);

        if (entry != null)
        {
            Candidates.Remove(entry);
        }

        AutoPickGovernor = DateTime.Now + TimeSpan.FromDays(Utility.RandomMinMax(2, 4));
    }

    public void Serialize(IGenericWriter writer)
    {
        writer.Write(0);

        writer.Write(ElectionEnded);
        writer.Write(AutoPickGovernor);

        writer.Write(StartTimes.Length);
        foreach (var dt in StartTimes)
        {
            writer.Write(dt);
        }

        writer.Write(Candidates.Count);
        foreach (var entry in Candidates)
        {
            entry.Serialize(writer);
        }
    }

    public void Deserialize(IGenericReader reader)
    {
        Candidates = new List<BallotEntry>();

        reader.ReadInt(); // version

        ElectionEnded = reader.ReadBool();
        AutoPickGovernor = reader.ReadDateTime();

        var count = reader.ReadInt();
        StartTimes = new DateTime[count];

        for (var i = 0; i < count; i++)
        {
            var time = reader.ReadDateTime();

            if (time < DateTime.Now && ElectionEnded)
            {
                time = new DateTime(time.Year + 1, time.Month, time.Day);
            }

            StartTimes[i] = time;
        }

        count = reader.ReadInt();

        for (var i = 0; i < count; i++)
        {
            var entry = BallotEntry.Load(reader);

            if (entry.Player != null)
            {
                Candidates.Add(entry);
            }
        }
    }
}

public class BallotEntry : IComparable<BallotEntry>
{
    public BallotEntry(PlayerMobile pm, int love, int hate)
    {
        Player = pm;
        TimeOfNomination = DateTime.Now;

        Love = love;
        Hate = hate;

        Endorsements = new List<PlayerMobile>();
        Votes = new List<PlayerMobile>();
    }

    private BallotEntry()
    {
        Endorsements = new List<PlayerMobile>();
        Votes = new List<PlayerMobile>();
    }

    public PlayerMobile Player { get; set; }
    public DateTime TimeOfNomination { get; set; }

    public int Love { get; set; }
    public int Hate { get; set; }

    public List<PlayerMobile> Endorsements { get; set; }
    public List<PlayerMobile> Votes { get; set; }

    public int CompareTo(BallotEntry entry)
    {
        if (Player == null)
        {
            return -1;
        }

        if (Votes.Count > entry.Votes.Count)
        {
            return 1;
        }

        if (Votes.Count < entry.Votes.Count)
        {
            return -1;
        }

        if (Love > entry.Love || Hate < entry.Hate)
        {
            return 1;
        }

        return Love == entry.Love && Hate == entry.Hate && Utility.RandomBool() ? 1 : -1;
    }

    public void Serialize(IGenericWriter writer)
    {
        writer.Write(0);

        writer.Write(Player);
        writer.Write(TimeOfNomination);

        writer.Write(Love);
        writer.Write(Hate);

        writer.Write(Endorsements.Count);
        foreach (var p in Endorsements)
        {
            writer.Write(p);
        }

        writer.Write(Votes.Count);
        foreach (var p in Votes)
        {
            writer.Write(p);
        }
    }

    public static BallotEntry Load(IGenericReader reader)
    {
        var entry = new BallotEntry();

        reader.ReadInt(); // version

        entry.Player = reader.ReadEntity<Mobile>() as PlayerMobile;
        entry.TimeOfNomination = reader.ReadDateTime();

        entry.Love = reader.ReadInt();
        entry.Hate = reader.ReadInt();

        var count = reader.ReadInt();
        for (var i = 0; i < count; i++)
        {
            if (reader.ReadEntity<Mobile>() is PlayerMobile p)
            {
                entry.Endorsements.Add(p);
            }
        }

        count = reader.ReadInt();
        for (var i = 0; i < count; i++)
        {
            if (reader.ReadEntity<Mobile>() is PlayerMobile p)
            {
                entry.Votes.Add(p);
            }
        }

        return entry;
    }
}
