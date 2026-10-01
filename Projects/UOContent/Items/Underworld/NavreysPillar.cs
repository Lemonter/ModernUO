using System;
using ModernUO.Serialization;

namespace Server.Items;

// Ported from real OSI/ServUO content (Scripts/Services/Dungeons/Underworld/Navrey's Lair/NavreysPillar.cs).
public enum NavreysPillarState
{
    Off,
    On,
    Hot
}

public enum PillarType
{
    Three = 1,
    Six,
    Nine
}

[SerializationGenerator(0, false)]
public partial class NavreysPillar : Item
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private PillarType _type;

    [SerializableField(1)]
    private NavreysController _controller;

    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private NavreysPillarState _state;

    private Timer _timer;
    private int _ticks;

    [SerializableFieldChanged(2)]
    private void OnStateChanged(NavreysPillarState oldValue, NavreysPillarState newValue)
    {
        _timer?.Stop();
        _timer = null;

        switch (newValue)
        {
            case NavreysPillarState.Off:
                Hue = 0x456;
                break;
            case NavreysPillarState.On:
                Hue = 0;
                break;
            case NavreysPillarState.Hot:
                _timer = Timer.DelayCall(TimeSpan.Zero, TimeSpan.FromSeconds(0.5), OnTick);
                _ticks = 6 * (int)Type;
                break;
        }
    }

    [Constructible]
    public NavreysPillar() : this(null, PillarType.Three)
    {
    }

    public NavreysPillar(NavreysController controller, PillarType type) : base(0x3BF)
    {
        _controller = controller;
        _type = type;
        Movable = false;
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (from.InRange(this, 3) && _state == NavreysPillarState.On)
        {
            State = NavreysPillarState.Hot;
            _controller?.CheckPillars();
        }
    }

    private void OnTick()
    {
        _ticks--;

        Hue = 0x461 + _ticks % 2;

        if (_ticks == 0)
        {
            _timer?.Stop();
            _timer = null;
            State = NavreysPillarState.On;
        }
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        if (_state == NavreysPillarState.Hot)
        {
            State = NavreysPillarState.On;
        }
    }
}
