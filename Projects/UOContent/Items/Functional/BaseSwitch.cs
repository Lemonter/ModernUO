using System;
using ModernUO.Serialization;
using Server.Network;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class BaseSwitch : Item
{
    [SerializableField(0)]
    private int _turnOn;

    [SerializableField(1)]
    private int _turnOff;

    [SerializableField(2)]
    private int _locMessageA;

    [SerializableField(3)]
    private int _locMessageB;

    [SerializableField(4)]
    private bool _working;

    [SerializableField(5)]
    private bool _used;

    [Constructible]
    public BaseSwitch(int turnOff = 0, int turnOn = 0, int locMessageA = 0, int locMessageB = 0, bool working = false)
        : base(turnOff)
    {
        Movable = false;
        _turnOn = turnOn;
        _turnOff = turnOff;
        _locMessageA = locMessageA;
        _locMessageB = locMessageB;
        _working = working;
    }

    public override void OnDoubleClick(Mobile m)
    {
        if (!m.InRange(this, 2))
        {
            m.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
            return;
        }

        var messageA = _locMessageA == 0 ? 500357 + Utility.Random(5) : _locMessageA;
        var messageB = _locMessageB == 0 ? 500357 + Utility.Random(5) : _locMessageB;

        if (ItemID == _turnOff && !_used)
        {
            ItemID = _turnOn;
            _used = true;
            Effects.PlaySound(Location, Map, 0x3E8);

            // This is the message received when it is turned on for the first time.
            m.LocalOverheadMessage(MessageType.Regular, 0, messageA);

            if (_working)
            {
                DoSomethingSpecial(m);
            }

            Timer.StartTimer(TimeSpan.FromMinutes(2.0), ResetSwitch);
        }
        else if (ItemID == _turnOff && _used)
        {
            ItemID = _turnOn;
            Effects.PlaySound(Location, Map, 0x3E8);
            m.LocalOverheadMessage(MessageType.Regular, 0, messageB);
        }
        else
        {
            ItemID = _turnOff;
            Effects.PlaySound(Location, Map, 0x3E8);
            m.LocalOverheadMessage(MessageType.Regular, 0, messageB);
        }
    }

    private void ResetSwitch()
    {
        ItemID = _turnOff;
        _used = false;
    }

    public virtual void DoSomethingSpecial(Mobile from) =>
        from.LocalOverheadMessage(MessageType.Regular, 0, 1116629); // It does Nothing!
}
