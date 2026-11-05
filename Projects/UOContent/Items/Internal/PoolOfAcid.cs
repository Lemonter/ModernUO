using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class PoolOfAcid : Item
{
    private readonly TimeSpan _duration;
    private readonly int _minDamage;
    private readonly int _maxDamage;
    private readonly DateTime _created;
    private bool _drying;

    private TimerExecutionToken _timerToken;

    [Constructible]
    public PoolOfAcid() : this(TimeSpan.FromSeconds(10.0), 2, 5)
    {
    }

    public PoolOfAcid(TimeSpan duration, int minDamage, int maxDamage) : base(0x122A)
    {
        Hue = 0x3F;
        Movable = false;
        _minDamage = minDamage;
        _maxDamage = maxDamage;
        _created = Core.Now;
        _duration = duration;

        Timer.StartTimer(TimeSpan.Zero, TimeSpan.FromSeconds(1), OnTick, out _timerToken);
    }

    public override string DefaultName => "лужа кислоты";

    public override void OnAfterDelete()
    {
        _timerToken.Cancel();

        base.OnAfterDelete();
    }

    public override bool OnMoveOver(Mobile m)
    {
        Damage(m);
        return true;
    }

    public void Damage(Mobile m) => m.Damage(Utility.RandomMinMax(_minDamage, _maxDamage));

    private void OnTick()
    {
        var age = Core.Now - _created;

        if (age > _duration)
        {
            Delete();
            return;
        }

        if (!_drying && age > _duration - age)
        {
            _drying = true;
            ItemID = 0x122B;
        }

        List<Mobile> toDamage = null;

        foreach (var m in GetMobilesInRange(0))
        {
            if (m.Alive && !m.IsDeadBondedPet && (m is not BaseCreature bc || bc.Controlled || bc.Summoned))
            {
                (toDamage ??= new List<Mobile>()).Add(m);
            }
        }

        if (toDamage == null)
        {
            return;
        }

        foreach (var m in toDamage)
        {
            Damage(m);
        }
    }
}
