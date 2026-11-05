using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Multis;

namespace Server.Items;

[Flippable(0x1173, 0x1174)]
[SerializationGenerator(0, false)]
public partial class AncestralGravestone : Item
{
    private const string ModName = "AncestralGravestone";

    private static readonly Dictionary<Mobile, SkillMod> _table = new();
    private static readonly Dictionary<Mobile, DateTime> _cooldown = new();
    private static TimerExecutionToken _timerToken;

    [Constructible]
    public AncestralGravestone() : base(0x1173)
    {
        LootType = LootType.Blessed;
    }

    public override int LabelNumber => 1071096; // Ancestral Gravestone

    public static bool UnderEffects(Mobile m) => _table.ContainsKey(m);

    public static bool IsInCooldown(Mobile m)
    {
        if (UnderEffects(m))
        {
            return true;
        }

        CheckCooldown();

        return _cooldown.ContainsKey(m);
    }

    public static void AddBonus(Mobile m)
    {
        var mod = new DefaultSkillMod(SkillName.SpiritSpeak, ModName, true, 5.0);
        _table[m] = mod;

        m.AddSkillMod(mod);
        _cooldown[m] = Core.Now + TimeSpan.FromMinutes(90);

        Timer.StartTimer(TimeSpan.FromMinutes(Utility.RandomMinMax(5, 40)), () => ExpireBonus(m, mod));
        StartCooldownTimer();
    }

    private static void ExpireBonus(Mobile m, SkillMod mod)
    {
        _table.Remove(m);

        if (m is { Deleted: false })
        {
            m.RemoveSkillMod(mod);
        }
    }

    private static void StartCooldownTimer()
    {
        if (_timerToken.Running)
        {
            return;
        }

        Timer.StartTimer(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1), CheckCooldown, out _timerToken);
    }

    public static void CheckCooldown()
    {
        if (_cooldown.Count == 0)
        {
            _timerToken.Cancel();
            return;
        }

        List<Mobile> expired = null;

        foreach (var (m, deadline) in _cooldown)
        {
            if (deadline <= Core.Now)
            {
                (expired ??= new List<Mobile>()).Add(m);
            }
        }

        if (expired == null)
        {
            return;
        }

        foreach (var m in expired)
        {
            _cooldown.Remove(m);
        }

        if (_cooldown.Count == 0)
        {
            _timerToken.Cancel();
        }
    }

    public override void OnDoubleClick(Mobile m)
    {
        var house = BaseHouse.FindHouseAt(this);

        if (house?.IsFriend(m) != true || !IsLockedDown && !IsSecure)
        {
            return;
        }

        if (IsInCooldown(m))
        {
            var remaining = _cooldown[m] - Core.Now;

            // Чтобы получить эффект снова, нужно подождать не менее ~1_VAL~ минут.
            m.SendLocalizedMessage(1071505, ((int)remaining.TotalMinutes).ToString());
            return;
        }

        AddBonus(m);
        m.FixedParticles(0x376A, 9, 32, 5030, EffectLayer.Waist);
    }
}
