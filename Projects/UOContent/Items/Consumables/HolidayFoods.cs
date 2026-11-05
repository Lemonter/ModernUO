using System;
using System.Collections.Generic;
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public abstract partial class BaseSweet : Food
{
    private static readonly Dictionary<Mobile, ToothAcheTimer> _toothAches = new();

    public BaseSweet(int itemID) : base(itemID)
    {
    }

    public BaseSweet(int amount, int itemID) : base(itemID, amount)
    {
    }

    public static Dictionary<Mobile, ToothAcheTimer> ToothAches => _toothAches;

    public virtual bool GivesToothAche => true;

    public virtual int Acidity => 32;

    public static void SetToothAche(Mobile from, int acidity, bool direct)
    {
        var timer = EnsureTimer(from, acidity);

        if (direct)
        {
            timer.Acidity = acidity;
        }
    }

    public static int GetToothAche(Mobile m) => _toothAches.TryGetValue(m, out var timer) ? timer.Acidity : 0;

    public static bool CureToothAche(Mobile m) => m != null && _toothAches.Remove(m);

    public override bool CheckHunger(Mobile from)
    {
        if (GivesToothAche)
        {
            EnsureTimer(from, Acidity);
        }

        from.SendLocalizedMessage(1077387); // You feel as if you could eat as much as you wanted!
        return true;
    }

    private static ToothAcheTimer EnsureTimer(Mobile m, int acidity)
    {
        if (!_toothAches.TryGetValue(m, out var timer))
        {
            timer = new ToothAcheTimer(m, acidity);
            _toothAches.Add(m, timer);
        }
        else
        {
            timer.Acidity += acidity;
        }

        if (!timer.Running)
        {
            timer.Start();
        }

        return timer;
    }

    public class ToothAcheTimer : Timer
    {
        public ToothAcheTimer(Mobile consumedBy, int acidity) : base(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30))
        {
            ConsumedBy = consumedBy;
            Acidity = acidity;

            Start();
        }

        public Mobile ConsumedBy { get; private set; }

        public int Acidity { get; set; }

        private bool Invalidate() =>
            ConsumedBy != null && _toothAches.ContainsKey(ConsumedBy) && !ConsumedBy.Deleted && Acidity > 0 &&
            ConsumedBy.Map != null && ConsumedBy.Map != Map.Internal && ConsumedBy.Alive;

        protected override void OnTick()
        {
            --Acidity;

            if (!Invalidate())
            {
                Stop();

                if (ConsumedBy != null)
                {
                    _toothAches.Remove(ConsumedBy);
                }

                return;
            }

            if (Acidity == 60)
            {
                ConsumedBy.SendLocalizedMessage(1077393); // The extreme pain in your teeth subsides.
                return;
            }

            if (Acidity <= 60)
            {
                return;
            }

            // ARRGH! My tooth hurts sooo much! ...
            ConsumedBy.Say(1077388 + Utility.Random(5));

            if (Utility.RandomBool() && ConsumedBy.Body.IsHuman && !ConsumedBy.Mounted)
            {
                ConsumedBy.Animate(32, 5, 1, true, false, 0);
            }
        }
    }
}
