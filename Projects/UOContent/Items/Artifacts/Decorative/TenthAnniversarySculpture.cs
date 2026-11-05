using ModernUO.Serialization;
using System;
using System.Collections.Generic;
using Server.Mobiles;
using Server.Accounting;
using Server.Engines.VeteranRewards;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Decorative/TenthAnniversarySculpture.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class TenthAnniversarySculpture : Item
{
    public override int LabelNumber => 1079532; // 10th Anniversary Sculpture

    private static Dictionary<Mobile, DateTime> _luckTable = new Dictionary<Mobile, DateTime>();

    private Dictionary<Mobile, DateTime> _rewardCooldown;

    public Dictionary<Mobile, DateTime> RewardCooldown { get { return _rewardCooldown; } }

    private static List<TenthAnniversarySculpture> _sculptures = new List<TenthAnniversarySculpture>();

    private static Timer _timer;

    private static readonly int MaxLuckBonus = 1000;

    [Constructible]
    public TenthAnniversarySculpture() : base( 15283 )
    {
        Weight = 1.0;
        _rewardCooldown = new Dictionary<Mobile, DateTime>();
        AddSculpture(this);
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!from.InRange(GetWorldLocation(), 2))
        {
            from.SendLocalizedMessage(500446); // That is too far away.
            return;
        }

        DefragTables();

        if (!IsCoolingDown(from))
        {
            _luckTable[from] = Core.Now + TimeSpan.FromMinutes(60);
            from.SendLocalizedMessage(1079551); // Your luck just improved!
            _rewardCooldown[from] = Core.Now + TimeSpan.FromHours(24);

            from.Delta(MobileDelta.Armor);
        }
    }

    public bool IsCoolingDown(Mobile from)
    {
        bool donemessage = false;

        if (_luckTable.ContainsKey(from))
        {
            from.SendLocalizedMessage(1079534); // You're still feeling lucky from the last time you touched the sculpture.
            donemessage = true;
        }

        foreach (TenthAnniversarySculpture sculpture in _sculptures)
        {
            if (sculpture.RewardCooldown != null && sculpture.RewardCooldown.ContainsKey(from))
            {
                if (!donemessage)
                {
                    TimeSpan left = sculpture.RewardCooldown[from] - Core.Now;

                    if (left.TotalHours > 1)
                    {
                        from.SendLocalizedMessage(1079550, ((int)left.TotalHours).ToString()); // You can improve your fortunes again in about ~1_TIME~ hours.
                    }
                    else if (left.TotalMinutes > 1)
                    {
                        from.SendLocalizedMessage(1079548, ((int)left.TotalMinutes).ToString()); // You can improve your fortunes in about ~1_TIME~ minutes.
                    }
                    else
                    {
                        from.SendLocalizedMessage(1079547); // Your fortunes are about to improve.
                    }
                }

                return true;
            }
        }

        return false;
    }

    public static void DefragTables()
    {
        foreach (TenthAnniversarySculpture sculpture in _sculptures)
        {
            List<Mobile> list = new List<Mobile>(sculpture.RewardCooldown.Keys);

            foreach (Mobile m in list)
            {
                if (sculpture.RewardCooldown.ContainsKey(m) && sculpture.RewardCooldown[m] < Core.Now)
                {
                    sculpture.RewardCooldown.Remove(m);
                }
            }

            list.Clear();
        }

        List<Mobile> remove = new List<Mobile>();
        foreach (KeyValuePair<Mobile, DateTime> kvp in _luckTable)
        {
            if (kvp.Value < Core.Now)
            {
                remove.Add(kvp.Key);
            }
        }

        remove.ForEach(m =>
        {
            _luckTable.Remove(m);

            if (m.NetState != null)
            {
                m.SendLocalizedMessage(1079552); //Your luck just ran out.
            }
        });

        remove.Clear();
    }

    public static int GetLuckBonus(Mobile from)
    {
        if (_luckTable.ContainsKey(from))
        {
            Account account = from.Account as Account;

            if(account != null)
            {
                return (int)Math.Min(MaxLuckBonus, 200 + (RewardSystem.GetRewardLevel(account)) * 50);
            }
        }

        return 0;
    }

    public override void Delete()
    {
        base.Delete();

        RemoveSculpture(this);

        if (_rewardCooldown != null)
        {
            _rewardCooldown.Clear();
        }
    }

    public static void AddSculpture(TenthAnniversarySculpture sculpture)
    {
        if (!_sculptures.Contains(sculpture))
        {
            _sculptures.Add(sculpture);
            StartTimer();
        }
    }

    public static void RemoveSculpture(TenthAnniversarySculpture sculpture)
    {
        if (_sculptures.Contains(sculpture))
        {
            _sculptures.Remove(sculpture);
        }

        if (_sculptures.Count == 0 && _timer != null)
        {
            _timer.Stop();
            _timer = null;
        }
    }

    public static void StartTimer()
    {
        if (_timer != null && _timer.Running)
        {
            return;
        }

        _timer = Timer.DelayCall(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1), DefragTables);
        _timer.Start();
    }
}
