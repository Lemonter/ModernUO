using System;
using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/ExploringTheDeep/Items/QuestItems/WillemHartesHat.cs). Recovered
///     from the Obsidian Wyvern in Destard and returned to Willem's mother, Madeline Harte. Like
///     <see cref="MercutiosCutlass" /> this is wearable equipment, so the original's hand-rolled lifespan
///     timer is kept (on a <see cref="TimerExecutionToken" />) instead of deriving from
///     <see cref="BaseDecayingItem" />.
/// </summary>
[SerializationGenerator(0, false)]
public partial class WillemHartesHat : FeatheredHat
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _timeLeft;

    private TimerExecutionToken _timer;

    [Constructible]
    public WillemHartesHat() : base(0x171A)
    {
        Hue = 72;
        StrRequirement = 10;
        QuestItem = true;

        if (Lifespan > 0)
        {
            _timeLeft = Lifespan;
            StartTimer();
        }
    }

    public int Lifespan => 3600;

    public override int LabelNumber => 1154236; // Willem Harte's Hat

    public override int BaseFireResistance => 5;
    public override int BaseColdResistance => 9;
    public override int BasePoisonResistance => 5;
    public override int BaseEnergyResistance => 5;

    public override int InitMinHits => 20;
    public override int InitMaxHits => 30;

    public override void OnDoubleClick(Mobile from)
    {
        base.OnDoubleClick(from);

        // *The hat emits a sour smelling odor indicative of spending a significant period of time in the
        // belly of a dragon.*
        from.PublicOverheadMessage(MessageType.Regular, 0x3B2, 1154237);
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (Lifespan > 0)
        {
            QuestItemLifespan.AddTo(list, _timeLeft);
        }
    }

    public override void OnAfterDelete()
    {
        _timer.Cancel();
        base.OnAfterDelete();
    }

    [AfterDeserialization]
    private void AfterDeserialization() => StartTimer();

    private void StartTimer()
    {
        if (_timer.Running)
        {
            return;
        }

        Timer.StartTimer(TimeSpan.FromSeconds(10.0), TimeSpan.FromSeconds(10.0), Slice, out _timer);
    }

    private void Slice()
    {
        _timeLeft -= 10;

        InvalidateProperties();

        if (_timeLeft <= 0)
        {
            Decay();
        }
    }

    private void Decay()
    {
        if (RootParent is Mobile parent)
        {
            parent.SendLocalizedMessage(1072515, Name ?? $"#{LabelNumber}"); // The ~1_name~ expired...

            Effects.SendLocationParticles(
                EffectItem.Create(parent.Location, parent.Map, EffectItem.DefaultDuration), 0x3728, 8, 20, 5042
            );
            Effects.PlaySound(parent.Location, parent.Map, 0x201);
        }
        else
        {
            Effects.SendLocationParticles(
                EffectItem.Create(Location, Map, EffectItem.DefaultDuration), 0x3728, 8, 20, 5042
            );
            Effects.PlaySound(Location, Map, 0x201);
        }

        _timer.Cancel();
        Delete();
    }
}
