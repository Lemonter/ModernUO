using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Gumps;

namespace Server.Items;

/// <summary>
///     Портировано из ServUO. Свойство SkillMod.ObeyCap в репозитории отсутствует, поэтому
///     бонус подчиняется обычному ограничению навыка.
/// </summary>
[SerializationGenerator(0, false)]
public partial class WoodworkersBench : BaseAddon
{
    public const int Cooldown = 45;
    public const int BonusDuration = 30;

    private static readonly Dictionary<Mobile, BenchBonus> _table = new();
    private static TimerExecutionToken _timerToken;

    [Constructible]
    public WoodworkersBench(bool east = true)
    {
        if (east)
        {
            AddComponent(new AddonComponent(0x19F2), 0, 0, 0);
            AddComponent(new AddonComponent(0x19F1), 0, 1, 0);
            AddComponent(new AddonComponent(0x19F3), 0, -1, 0);
        }
        else
        {
            AddComponent(new AddonComponent(0x19F6), 0, 0, 0);
            AddComponent(new AddonComponent(0x19F5), 1, 0, 0);
            AddComponent(new AddonComponent(0x19F7), -1, 0, 0);
        }
    }

    public override BaseAddonDeed Deed => new WoodworkersBenchDeed();

    public override void OnComponentUsed(AddonComponent c, Mobile m)
    {
        if (IsInCooldown(m))
        {
            var remaining = _table[m].Start + TimeSpan.FromMinutes(BonusDuration + Cooldown) - Core.Now;

            // In order to get a buff again, you have to wait for at least ~1_VAL~ minutes.
            m.SendLocalizedMessage(1071505, $"{remaining.Minutes}");
        }
        else if (HasBonus(m))
        {
            var remaining = _table[m].Start + TimeSpan.FromMinutes(BonusDuration) - Core.Now;

            // You already have the bench's buff. Time remaining of the buff: ~1_VAL~ min ~2_VAL~ sec
            m.SendLocalizedMessage(1071522, $"{remaining.Minutes}\t{remaining.Seconds}");
        }
        else
        {
            m.SendLocalizedMessage(1071504); // Carpentry will go smoothly now...

            AddBonus(m);
            m.FixedParticles(0x376A, 9, 32, 5030, EffectLayer.Waist);
        }
    }

    public static void AddBonus(Mobile m)
    {
        var mod = new DefaultSkillMod(SkillName.Carpentry, "WoodworkersBench", true, 5.0);
        m.AddSkillMod(mod);

        _table[m] = new BenchBonus(true, Core.Now, mod);

        if (!_timerToken.Running)
        {
            Timer.StartTimer(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1), CheckTable, out _timerToken);
        }
    }

    public static bool HasBonus(Mobile m, SkillName skill) => skill == SkillName.Carpentry && HasBonus(m);

    public static bool HasBonus(Mobile m) => _table.TryGetValue(m, out var bonus) && bonus.Active;

    public static bool IsInCooldown(Mobile m)
    {
        if (m.AccessLevel > AccessLevel.Player || !_table.TryGetValue(m, out var bonus))
        {
            return false;
        }

        if (bonus.Start + TimeSpan.FromMinutes(BonusDuration + Cooldown) < Core.Now)
        {
            _table.Remove(m);
            return false;
        }

        return bonus.Start + TimeSpan.FromMinutes(BonusDuration) < Core.Now;
    }

    public static void CheckTable()
    {
        if (_table.Count == 0)
        {
            _timerToken.Cancel();
            return;
        }

        List<Mobile> expired = null;

        foreach (var (m, bonus) in _table)
        {
            if (bonus.Start + TimeSpan.FromMinutes(BonusDuration + Cooldown) < Core.Now)
            {
                (expired ??= new List<Mobile>()).Add(m);
            }
            else if (bonus.Active && bonus.Start + TimeSpan.FromMinutes(BonusDuration) < Core.Now)
            {
                m.RemoveSkillMod(bonus.Mod);

                if (m.NetState != null)
                {
                    m.SendLocalizedMessage(1071506); // Your carpentry bonus was removed...
                }

                _table[m] = new BenchBonus(false, bonus.Start, null);
            }
        }

        if (expired == null)
        {
            return;
        }

        foreach (var m in expired)
        {
            _table.Remove(m);
        }

        if (_table.Count == 0)
        {
            _timerToken.Cancel();
        }
    }

    private readonly record struct BenchBonus(bool Active, DateTime Start, SkillMod Mod);
}

[SerializationGenerator(0, false)]
public partial class WoodworkersBenchDeed : BaseAddonDeed, IRewardOption
{
    [SerializableField(0)]
    private bool _east;

    [Constructible]
    public WoodworkersBenchDeed()
    {
        LootType = LootType.Blessed;
    }

    public override int LabelNumber => 1026641; // Woodworker's Bench
    public override BaseAddon Addon => new WoodworkersBench(_east);

    public void GetOptions(RewardOptionList list)
    {
        list.Add(0, 1071502); // Woodworker's Bench (South)
        list.Add(1, 1071503); // Woodworker's Bench (East)
    }

    public void OnOptionSelected(Mobile from, int choice)
    {
        _east = choice == 1;

        if (!Deleted)
        {
            base.OnDoubleClick(from);
        }
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1062334); // This item must be in your backpack to be used.
            return;
        }

        RewardOptionGump.DisplayTo(from, this);
    }
}
