using System;
using ModernUO.Serialization;
using Server.Engines.Harvest;
using Server.Items;

namespace Server.Items;

/// <summary>
///     Ported from ServUO's Covetous void pool (Items/SmeltersTalisman.cs, Items/WoodsmanTalisman.cs).
///     Both are the same object with a different crafting resource, starting uses and localized "type"
///     word, so the shared behaviour lives in this base instead of being copied twice.
///
///     Differences from the original: the decay check was a 10-second repeating timer; here it is a
///     single one-shot timer armed for the remaining lifespan (the tooltip derives the countdown from
///     Expires), cancelled in OnAfterDelete. ServUO's AddUsesRemainingProperties has no counterpart —
///     the "uses remaining" line is added from GetProperties instead.
/// </summary>
[SerializationGenerator(0, false)]
public abstract partial class VoidPoolTalisman : BaseTalisman, IUsesRemaining
{
    [SerializableProperty(0)]
    [CommandProperty(AccessLevel.GameMaster)]
    public CraftResource Resource
    {
        get => _resource;
        set
        {
            if (_resource == value)
            {
                return;
            }

            _resource = value;
            Hue = CraftResources.GetHue(value);
            InvalidateProperties();
        }
    }

    [SerializableProperty(1)]
    [CommandProperty(AccessLevel.GameMaster)]
    public int UsesRemaining
    {
        get => _usesRemaining;
        set
        {
            _usesRemaining = value;

            if (_usesRemaining <= 0 && RootParent is Mobile parent)
            {
                parent.SendLocalizedMessage(1152621); // Your talisman's magic is exhausted.
            }

            InvalidateProperties();
        }
    }

    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _showUsesRemaining;

    [SerializableField(3)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private DateTime _expires;

    private TimerExecutionToken _decayToken;

    protected VoidPoolTalisman(int itemID, CraftResource resource, int uses, int decayHours) : base(itemID)
    {
        Resource = resource;
        UsesRemaining = uses;
        _showUsesRemaining = true;
        _expires = Core.Now + TimeSpan.FromHours(decayHours);

        ScheduleDecay();
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();
        _decayToken.Cancel();
    }

    private void ScheduleDecay()
    {
        _decayToken.Cancel();

        var remaining = _expires - Core.Now;

        if (remaining <= TimeSpan.Zero)
        {
            Timer.DelayCall(Decay);
            return;
        }

        Timer.StartTimer(remaining, Decay, out _decayToken);
    }

    public void Decay()
    {
        if (RootParent is Mobile parent)
        {
            parent.SendLocalizedMessage(1072515, Name == null ? $"#{LabelNumber}" : Name); // The ~1_name~ expired...

            Effects.SendLocationParticles(
                EffectItem.Create(parent.Location, parent.Map, EffectItem.DefaultDuration),
                0x3728,
                8,
                20,
                5042
            );
            Effects.PlaySound(parent.Location, parent.Map, 0x201);
        }
        else
        {
            Effects.SendLocationParticles(
                EffectItem.Create(Location, Map, EffectItem.DefaultDuration),
                0x3728,
                8,
                20,
                5042
            );
            Effects.PlaySound(Location, Map, 0x201);
        }

        Delete();
    }

    public override void AddNameProperty(IPropertyList list)
    {
        list.Add(1152599, $"#{CraftResources.GetLocalizationNumber(_resource)}\t#{TypeLocalization}"); // ~1_RES~ ~2_TYPE~ Talisman
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (_usesRemaining > 0)
        {
            list.Add(1060584, _usesRemaining.ToString()); // uses remaining: ~1_val~
        }

        var left = _expires > Core.Now ? (int)(_expires - Core.Now).TotalSeconds : 0;
        list.Add(1072517, left.ToString()); // Lifespan: ~1_val~ seconds
    }

    [AfterDeserialization(false)]
    private void AfterDeserialization()
    {
        if (_expires <= Core.Now)
        {
            Timer.DelayCall(Decay);
        }
        else
        {
            ScheduleDecay();
        }
    }

    /// <summary>Localized "smelter's" / "woodsman's" word used by the shared name template.</summary>
    protected abstract int TypeLocalization { get; }
}

[SerializationGenerator(0, false)]
public partial class SmeltersTalisman : VoidPoolTalisman
{
    public const int DecayPeriod = 24;

    protected override int TypeLocalization => 1152606;

    [Constructible]
    public SmeltersTalisman() : this(CraftResource.Iron)
    {
    }

    public SmeltersTalisman(CraftResource resource) : base(0x2F5B, resource, 6000, DecayPeriod)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class WoodsmansTalisman : VoidPoolTalisman
{
    public const int DecayPeriod = 24;

    protected override int TypeLocalization => 1152605;

    [Constructible]
    public WoodsmansTalisman() : this(CraftResource.RegularWood)
    {
    }

    public WoodsmansTalisman(CraftResource resource) : base(0x2F5A, resource, 300, DecayPeriod)
    {
    }

    /// <summary>Called from the lumberjacking harvest system: while the talisman matches the wood
    /// being cut it burns a use and yields extra logs.</summary>
    public static int CheckHarvest(Mobile from, Type type, HarvestSystem system)
    {
        if (system != Lumberjacking.System)
        {
            return 0;
        }

        var resource = CraftResources.GetFromType(type);

        if (from.FindItemOnLayer(Layer.Talisman) is not WoodsmansTalisman talisman ||
            talisman.UsesRemaining <= 0 || resource != talisman.Resource)
        {
            return 0;
        }

        talisman.UsesRemaining--;
        from.SendLocalizedMessage(1152625); // The magic of your talisman guides your hands as you cut the wood.

        return Utility.RandomMinMax(2, 5);
    }
}
