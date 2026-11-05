using System;
using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfSorceresDungeon/Items/ShackledHeartOfThePumpkinKing.cs).
///     The drain timer is a token-based repeat timer cancelled in <see cref="OnAfterDelete" />.
/// </summary>
[SerializationGenerator(0, false)]
public partial class ShackledHeartOfThePumpkinKing : Item
{
    private Mobile _owner;
    private int _ticks;
    private TimerExecutionToken _timerToken;

    public override int LabelNumber => 1157653; // Shackled Heart of the Pumpkin King

    [Constructible]
    public ShackledHeartOfThePumpkinKing() : base(0x4A9C)
    {
    }

    public override void OnDoubleClick(Mobile m)
    {
        if (_owner != null)
        {
            m.SendLocalizedMessage(1157630); // You are already bleeding!
            return;
        }

        m.SendLocalizedMessage(1157631); // Thou shall know the pain of a chained heart...
        m.SendSound(Utility.RandomMinMax(0x423, 0x427));

        _owner = m;
        _ticks = 0;

        Timer.StartTimer(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2), OnTick, out _timerToken);
    }

    private void OnTick()
    {
        if (_owner == null || !_owner.Alive || _owner.Map == Map.Internal || _ticks++ >= 5)
        {
            Stop();

            _owner = null;
            return;
        }

        _owner.PlaySound(0x133);

        var blood = new Blood
        {
            ItemID = Utility.Random(0x122A, 5)
        };

        blood.MoveToWorld(_owner.Location, _owner.Map);
    }

    private void Stop()
    {
        _timerToken.Cancel();
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();

        _timerToken.Cancel();
        _owner = null;
    }
}
