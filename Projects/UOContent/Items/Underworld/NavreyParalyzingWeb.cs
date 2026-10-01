using System;
using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Items;

// Ported from JustUO (github.com/JustUO/JustUO, a ServUO fork) — see
// Regions/UnderworldRegion.cs header for why ServUO itself wasn't the source here.

[Flippable(0x0EE3, 0x0EE4, 0x0EE5, 0x0EE6)]
[SerializationGenerator(0, false)]
public partial class NavreyParalyzingWeb : Item
{
    private static readonly TimeSpan _duration = TimeSpan.FromSeconds(60.0);

    [SerializableField(0)]
    private DateTime _end;

    private Timer _timer;

    [Constructible]
    public NavreyParalyzingWeb() : base(0x0EE3 + Utility.Random(4))
    {
        Visible = true;
        Movable = false;

        _end = Core.Now + _duration;
        StartTimer(_duration);
    }

    public override bool BlocksFit => true;

    private void StartTimer(TimeSpan delay)
    {
        _timer?.Stop();
        _timer = Timer.DelayCall(delay, Delete);
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();

        _timer?.Stop();
        _timer = null;

        if (Map == null)
        {
            return;
        }

        // remove paralyze from all chars in this location
        foreach (var m in Map.GetMobilesInRange(Location, 0))
        {
            m.Paralyzed = false;
        }
    }

    public override bool OnMoveOver(Mobile m)
    {
        if (m is Navrey)
        {
            return true;
        }

        if (m.AccessLevel == AccessLevel.Player)
        {
            m.Paralyze(_duration);

            m.PlaySound(0x204);
            m.FixedEffect(0x376A, 10, 16);
        }

        return true;
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        var remaining = _end - Core.Now;
        StartTimer(remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero);
    }
}
