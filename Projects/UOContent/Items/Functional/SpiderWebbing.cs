using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Engines.BuffIcons;
using Server.Mobiles;

namespace Server.Items;

/// <summary>
///     Портировано из ServUO. Таймер паутины хранится в TimerExecutionToken, бафф выдаётся
///     через PlayerMobile (как принято в репозитории).
/// </summary>
[SerializationGenerator(0, false)]
public partial class SpiderWebbing : Item
{
    private static readonly List<Mobile> _webVictims = new();

    private Mobile _target;
    private int _ticks;
    private TimerExecutionToken _timerToken;

    [Constructible]
    public SpiderWebbing() : base(0xEE3 + Utility.Random(4))
    {
        Movable = false;
    }

    public SpiderWebbing(Mobile m) : this()
    {
        BeginWebbing(m);

        _target = m;
        _ticks = 10;
        Timer.StartTimer(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(1.0), OnTick, out _timerToken);
    }

    public void BeginWebbing(Mobile m)
    {
        m.RevealingAction();
        m.Frozen = true;
        // You are wrapped in spider webbing and cannot move!
        m.SendLocalizedMessage(1113247);
        _webVictims.Add(m);
        (m as PlayerMobile)?.AddBuff(new BuffInfo(BuffIcon.Webbing, 1153789, 1153825));
    }

    public static bool IsTrapped(Mobile m) => _webVictims.Contains(m);

    public static void RemoveEffects(Mobile m)
    {
        m.Frozen = false;
        m.SendLocalizedMessage(1113248); // You escape the spider's web!
        (m as PlayerMobile)?.RemoveBuff(BuffIcon.Webbing);
        _webVictims.Remove(m);
    }

    public override bool BlocksFit => true;

    private void OnTick()
    {
        _ticks--;

        if (_target?.Alive != true || _ticks == 0)
        {
            Delete();
        }
    }

    [AfterDeserialization]
    private void AfterDeserialization() => Delete();

    public override void OnDelete()
    {
        _timerToken.Cancel();

        if (_webVictims.Count > 0)
        {
            var list = new List<Mobile>(_webVictims);

            foreach (var m in list)
            {
                RemoveEffects(m);
            }

            _webVictims.Clear();
        }

        base.OnDelete();
    }

    public override bool OnMoveOver(Mobile m)
    {
        if (m is BaseCreature { IsMonster: true })
        {
            return true;
        }

        if (m.AccessLevel == AccessLevel.Player && m.Alive)
        {
            BeginWebbing(m);
            m.PlaySound(0x204);
            m.FixedEffect(0x376A, 10, 16);
        }

        return true;
    }
}
