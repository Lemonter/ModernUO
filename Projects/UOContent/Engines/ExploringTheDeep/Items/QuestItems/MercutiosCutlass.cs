using System;
using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/ExploringTheDeep/Items/QuestItems/MercutiosCutlass.cs). The
///     proof Champ Huthwait wants that Mercutio is dead; it is a usable cutlass that rots away after five
///     in-game hours. ServUO hand-rolled the lifespan timer because the item is a weapon; that is kept,
///     but on <see cref="TimerExecutionToken" /> so <see cref="OnAfterDelete" /> can cancel it.
/// </summary>
[SerializationGenerator(0, false)]
public partial class MercutiosCutlass : Cutlass
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _timeLeft;

    private TimerExecutionToken _timer;

    [Constructible]
    public MercutiosCutlass()
    {
        Hue = 2595;
        LootType = LootType.Blessed;
        QuestItem = true;

        if (Lifespan > 0)
        {
            _timeLeft = Lifespan;
            StartTimer();
        }
    }

    public int Lifespan => 18000;

    public override int LabelNumber => 1154240; // Mercutio's Cutlass

    public override void OnDoubleClick(Mobile from)
    {
        base.OnDoubleClick(from);

        // *It is a beautifully and masterfully crafted blade. The hilt bears the family crest of the
        // former owner*
        from.PublicOverheadMessage(MessageType.Regular, 0x3B2, 1154241);
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

    public override void GetDamageTypes(
        Mobile wielder, out int phys, out int fire, out int cold, out int pois, out int nrgy, out int chaos,
        out int direct
    )
    {
        cold = fire = chaos = pois = nrgy = direct = 0;
        phys = 100;
    }
}
