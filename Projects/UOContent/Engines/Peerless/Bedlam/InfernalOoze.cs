using System;
using ModernUO.Serialization;

namespace Server.Items;

/// <summary>Ported from ServUO, where it lives inside
/// Scripts/Mobiles/Bosses/MonstrousInterredGrizzle.cs. The acid the Grizzle spits across its
/// arena: a corrosive pool either eats your equipment or burns you outright, thickens after
/// half a minute and dries up five seconds later.
///
/// Not ported: the BalmOfProtection mitigation branch, since that Eodon potion has no class in
/// this codebase. Add the check to ApplyTo() when it lands.</summary>
[SerializationGenerator(0, false)]
public partial class InfernalOoze : Item
{
    [SerializableField(0)]
    private Mobile _owner;

    [SerializableField(1)]
    private bool _corrosive;

    [SerializableField(2)]
    private int _damage;

    private TimerExecutionToken _token;
    private int _ticks;

    [Constructible]
    public InfernalOoze(Mobile owner = null, bool corrosive = false, int damage = 40) : base(0x122A)
    {
        Movable = false;
        Hue = 0x95;

        _owner = owner;
        _corrosive = corrosive;
        _damage = damage;

        StartTimer();
    }

    public override bool OnMoveOver(Mobile m)
    {
        // Only players and their followers step in it; the boss's own helpers are immune.
        if (m is Mobiles.PlayerMobile or Mobiles.BaseCreature { Controlled: true })
        {
            ApplyTo(m);
        }

        return true;
    }

    private void StartTimer()
    {
        _token.Cancel();
        Timer.StartTimer(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), OnTick, out _token);
    }

    private void OnTick()
    {
        if (Deleted)
        {
            _token.Cancel();
            return;
        }

        _ticks++;

        if (_ticks == 30)
        {
            ItemID++;
        }
        else if (_ticks >= 35)
        {
            Delete();
            return;
        }

        var map = Map;

        if (map == null)
        {
            return;
        }

        foreach (var m in map.GetMobilesAt(Location))
        {
            if (m is Mobiles.PlayerMobile or Mobiles.BaseCreature { Controlled: true })
            {
                ApplyTo(m);
            }
        }
    }

    private void ApplyTo(Mobile m)
    {
        if (_corrosive)
        {
            // Eats durability off whatever it can reach rather than health.
            foreach (var item in m.Items)
            {
                if (item is not IDurability durable || durable.HitPoints <= 0)
                {
                    continue;
                }

                if (0.25 < Utility.RandomDouble())
                {
                    continue;
                }

                durable.HitPoints -= durable.HitPoints > 10 ? 10 : 1;
            }
        }
        else
        {
            AOS.Damage(m, _owner, _damage, false, 0, 0, 0, 0, 0, 0, 100);
        }
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();

        _token.Cancel();
        _owner = null;
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        StartTimer();
    }
}
