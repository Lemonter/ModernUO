using System;
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public abstract partial class BaseDecayingItem : Item
{
    private Timer _timer;

    public virtual int Lifespan => 0;
    public virtual bool UseSeconds => true;

    [InvalidateProperties]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _timeLeft;

    public BaseDecayingItem(int itemID) : base(itemID)
    {
        LootType = LootType.Blessed;

        if (Lifespan > 0)
        {
            _timeLeft = Lifespan;
            StartTimer();
        }
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (Lifespan <= 0)
        {
            return;
        }

        if (UseSeconds)
        {
            list.Add(1072517, _timeLeft.ToString()); // Lifespan: ~1_val~ seconds
            return;
        }

        var t = TimeSpan.FromSeconds(TimeLeft);

        if (t.Days / 7 > 1)
        {
            list.Add(1153092, (t.Days / 7).ToString()); // Lifespan: ~1_val~ weeks
        }
        else if (t.Days > 1)
        {
            list.Add(1153091, t.Days.ToString()); // Lifespan: ~1_val~ days
        }
        else if (t.Hours > 1)
        {
            list.Add(1153090, t.Hours.ToString()); // Lifespan: ~1_val~ hours
        }
        else if (t.Minutes > 1)
        {
            list.Add(1153089, t.Minutes.ToString()); // Lifespan: ~1_val~ minutes
        }
        else
        {
            list.Add(1072517, t.Seconds.ToString()); // Lifespan: ~1_val~ seconds
        }
    }

    public virtual void StartTimer()
    {
        if (_timer != null || Lifespan == 0)
        {
            return;
        }

        _timer = Timer.DelayCall(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10), Slice);
    }

    public virtual void StopTimer()
    {
        _timer?.Stop();
        _timer = null;
    }

    public virtual void Slice()
    {
        TimeLeft -= 10;

        if (_timeLeft <= 0)
        {
            Decay();
        }
    }

    public virtual void Decay()
    {
        if (RootParent is Mobile parent)
        {
            parent.SendLocalizedMessage(1072515, Name ?? "#" + LabelNumber); // The ~1_name~ expired...

            Effects.SendLocationParticles(EffectItem.Create(parent.Location, parent.Map, EffectItem.DefaultDuration), 0x3728, 8, 20, 5042);
            Effects.PlaySound(parent.Location, parent.Map, 0x201);
        }
        else
        {
            Effects.SendLocationParticles(EffectItem.Create(Location, Map, EffectItem.DefaultDuration), 0x3728, 8, 20, 5042);
            Effects.PlaySound(Location, Map, 0x201);
        }

        StopTimer();
        Delete();
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();
        StopTimer();
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        if (Lifespan > 0)
        {
            StartTimer();
        }
    }
}
