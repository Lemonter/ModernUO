using System;
using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfKotlCity/Items/WheelsOfTime.cs).
///     Dropped: the <c>KotlBattleSimulator.EndSimulation</c> call (the simulator is not ported), so letting the time
///     warp expire only lowers the rock barrier back. <c>DateTime.UtcNow</c> became <c>Core.Now</c>, and the refill
///     timer is a <see cref="TimerExecutionToken" /> cancelled in <c>OnAfterDelete</c>.
/// </summary>
[SerializationGenerator(0, false)]
public partial class WheelsOfTime : Item
{
    public static TimeSpan TimeWarpDuration = TimeSpan.FromHours(2);

    public static Point3D RockBarrierLocation = new(584, 2399, 0);

    public static WheelsOfTime Instance { get; set; }

    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private BaseAddon _rockBarrier;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private DateTime _timeWarpEnds;

    private TimerExecutionToken _timerToken;

    public override int LabelNumber => 1157032; // Wheels of Time

    [Constructible]
    public WheelsOfTime() : base(0x9CEF)
    {
        Hue = 2655;
        Movable = false;
        Instance = this;

        _rockBarrier = new KotlWallAddon();
        _rockBarrier.MoveToWorld(RockBarrierLocation, Map.TerMur);

        _timeWarpEnds = DateTime.MinValue;
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        Instance = this;

        if (_rockBarrier == null)
        {
            _rockBarrier = new KotlWallAddon();
            _rockBarrier.MoveToWorld(RockBarrierLocation, Map.TerMur);
        }

        if (_timeWarpEnds == DateTime.MinValue)
        {
            return;
        }

        if (_timeWarpEnds > Core.Now)
        {
            Timer.StartTimer(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1), OnTick, out _timerToken);
        }
        else
        {
            Timer.StartTimer(TimeSpan.FromSeconds(60), Cleanup, out _timerToken);
        }
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!from.InRange(GetWorldLocation(), 1))
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
        }
        else if (_timeWarpEnds != DateTime.MinValue)
        {
            from.SendLocalizedMessage(1157021); // The wheels of time have already been set in motion.
        }
        else
        {
            from.PrivateOverheadMessage(MessageType.Regular, 1154, 1157020, from.NetState);
            from.PlaySound(0x0FF);

            ActivateTimeWarp();
        }
    }

    public void ActivateTimeWarp()
    {
        _rockBarrier?.MoveToWorld(new Point3D(_rockBarrier.X, _rockBarrier.Y, _rockBarrier.Z - 50), Map.TerMur);

        _timeWarpEnds = Core.Now + TimeWarpDuration;

        Timer.StartTimer(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1), OnTick, out _timerToken);
    }

    private void OnTick()
    {
        if (_timeWarpEnds >= Core.Now)
        {
            return;
        }

        _timerToken.Cancel();
        Cleanup();
    }

    private void Cleanup()
    {
        _timeWarpEnds = DateTime.MinValue;

        if (_rockBarrier != null && _rockBarrier.Location != RockBarrierLocation)
        {
            _rockBarrier.MoveToWorld(RockBarrierLocation, Map.TerMur);
        }
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();

        _timerToken.Cancel();
    }
}
