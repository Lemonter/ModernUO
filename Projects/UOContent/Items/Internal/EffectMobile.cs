using System;
using System.Collections.Generic;
using ModernUO.Serialization;

namespace Server.Mobiles;

[SerializationGenerator(0, false)]
public partial class EffectMobile : Mobile
{
    public static readonly TimeSpan DefaultDuration = TimeSpan.FromSeconds(1.0);

    private static readonly List<EffectMobile> _free = new();

    private TimerExecutionToken _freeToken;

    private EffectMobile()
    {
        CantWalk = true;
        Hidden = true;
    }

    public static EffectMobile Create(Point3D p, Map map, TimeSpan duration)
    {
        EffectMobile mobile = null;

        for (var i = _free.Count - 1; mobile == null && i >= 0; --i)
        {
            var free = _free[i];

            _free.RemoveAt(i);

            if (!free.Deleted && free.Map == Map.Internal)
            {
                mobile = free;
            }
        }

        mobile ??= new EffectMobile();

        mobile.MoveToWorld(p, map);
        mobile.BeginFree(duration);

        return mobile;
    }

    public void BeginFree(TimeSpan duration) => Timer.StartTimer(duration, Free, out _freeToken);

    private void Free()
    {
        Internalize();
        _free.Add(this);
    }

    [AfterDeserialization]
    private void AfterDeserialization() => Delete();

    public override void OnAfterDelete()
    {
        _freeToken.Cancel();

        base.OnAfterDelete();
    }
}
