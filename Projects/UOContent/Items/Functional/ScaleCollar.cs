using System;
using ModernUO.Serialization;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class ScaleCollar : Item
{
    private Mobile _owner;
    private BaseCreature _lizard;
    private DateTime _endTime;
    private TimerExecutionToken _timerToken;

    [Constructible]
    public ScaleCollar() : base(4235)
    {
        Hue = 2125;
    }

    public override int LabelNumber => 1112480; // a scale collar

    private bool TimerRunning => _timerToken.Running;

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042004); // That must be in your pack for you to use it.
        }
        else if (TimerRunning)
        {
            from.SendLocalizedMessage(501789); // You must wait before trying again.
        }
        else
        {
            from.Target = new InternalTarget(this);
            from.SendLocalizedMessage(1112481); // Which battle chicken do you wish to ensnare?
        }
    }

    public void OnTarget(Mobile from, object targeted)
    {
        if (targeted is not BattleChickenLizard bcl || bcl.Controlled)
        {
            return;
        }

        var chance = 50 / (int)Math.Max(1, from.GetDistanceToSqrt(bcl.Location));

        if (chance > Utility.Random(100))
        {
            bcl.Frozen = true;

            _lizard = bcl;
            _owner = from;
            _endTime = Core.Now + TimeSpan.FromSeconds(30);

            bcl.FixedEffect(0x376A, 1, 32);
            Timer.StartTimer(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), OnTick, out _timerToken);

            // You successfully ensnare the chicken! You best hurry before it frees itself from it!
            from.SendLocalizedMessage(1112484);
        }
        else
        {
            // The collar falls to the ground as the chicken deftly avoids it.
            from.SendLocalizedMessage(1112483);
        }
    }

    private void OnTick()
    {
        if (_lizard == null)
        {
            EndTimer();
            return;
        }

        if (_lizard.Controlled)
        {
            _lizard.Frozen = false;
            EndTimer();
            return;
        }

        if (_endTime < Core.Now)
        {
            EndTimer();
            return;
        }

        _lizard.FixedEffect(0x376A, 1, 32);
    }

    private void EndTimer()
    {
        if (_lizard?.Alive == true)
        {
            _lizard.Frozen = false;

            if (_owner != null && !_lizard.Controlled)
            {
                // The chicken frees itself of the collar!!
                _owner.SendLocalizedMessage(1112482);
            }
        }

        _timerToken.Cancel();
        _lizard = null;
        _owner = null;
    }

    private class InternalTarget : Target
    {
        private readonly ScaleCollar _collar;

        public InternalTarget(ScaleCollar collar) : base(-1, false, TargetFlags.None) => _collar = collar;

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (!_collar.Deleted)
            {
                _collar.OnTarget(from, targeted);
            }
        }
    }
}
