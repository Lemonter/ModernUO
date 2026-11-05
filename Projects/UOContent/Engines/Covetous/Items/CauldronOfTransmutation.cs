using System;
using ModernUO.Serialization;
using Server.Multis;
using Server.Targeting;

namespace Server.Items;

/// <summary>
///     Ported from ServUO's Covetous void pool (Items/CauldronOfTransmutation.cs) — the cauldron that
///     turns three ingots/logs of one resource into one of the next grade, 5000 charges per four hours.
///     Bought with void pool points, house-only, usable by anyone the house allows as co-owner.
///
///     Adapted the same way as the temporary forge: the 10-second decay poll became a single timer armed
///     for the remaining lifespan (cancelled in OnAfterDelete), and the properties moved to
///     IPropertyList. The cauldron's Resource is inherited from the deed, since BaseAddonDeed here does
///     not copy it during placement.
/// </summary>
[SerializationGenerator(0, false)]
public partial class CauldronOfTransmutation : BaseAddon
{
    public const int DecayPeriod = 4;

    [SerializableProperty(0)]
    [CommandProperty(AccessLevel.GameMaster)]
    public int Charges
    {
        get => _charges;
        set
        {
            _charges = value;

            if (_charges <= 0 && RootParent is Mobile parent)
            {
                parent.SendLocalizedMessage(1152635); // The cauldron's magic is exhausted
            }

            InvalidateProperties();
        }
    }

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private DateTime _expires;

    private TimerExecutionToken _decayToken;

    public override bool RetainDeedHue => true;

    [Constructible]
    public CauldronOfTransmutation()
    {
        Charges = 5000;

        AddComponent(new InternalComponent(2421), 0, 0, 0);

        _expires = Core.Now + TimeSpan.FromHours(DecayPeriod);
        ScheduleDecay();
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

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();
        _decayToken.Cancel();
    }

    public override void OnComponentUsed(AddonComponent c, Mobile from)
    {
        if (!from.InRange(c.Location, 3))
        {
            return;
        }

        var house = BaseHouse.FindHouseAt(from);

        if (house != null && house.IsCoOwner(from))
        {
            from.Target = new InternalTarget(this);
            from.SendLocalizedMessage(1152738); // Target ingots in your backpack to transmute them.
        }
    }

    public bool TryTransmutate(Mobile from, Item dropped)
    {
        var house = BaseHouse.FindHouseAt(from);

        if (house == null || !house.IsOwner(from))
        {
            from.SendLocalizedMessage(1152632); // That is not yours!
            return false;
        }

        var resource = CraftResources.GetFromType(dropped.GetType());

        if (resource != Resource)
        {
            return false;
        }

        if (dropped.Amount < 3)
        {
            from.SendLocalizedMessage(1152634); // There is not enough to transmute
            return false;
        }

        if (Charges <= 0)
        {
            from.SendLocalizedMessage(1152635); // The cauldron's magic is exhausted
            return false;
        }

        var info = CraftResources.GetInfo(Resource + 1);

        if (info == null || info.ResourceTypes.Length == 0)
        {
            from.SendLocalizedMessage(1152633); // The cauldron cannot transmute that
            return false;
        }

        var toDrop = Math.Min(Charges * 3, dropped.Amount);

        while (toDrop % 3 != 0)
        {
            toDrop--;
        }

        var newAmount = toDrop / 3;

        if (toDrop < dropped.Amount)
        {
            dropped.Amount -= toDrop;
        }
        else
        {
            dropped.Delete();
        }

        var item = Loot.Construct(info.ResourceTypes[0]);

        if (item == null)
        {
            return false;
        }

        item.Amount = newAmount;
        from.AddToBackpack(item);

        from.SendLocalizedMessage(1152636); // The cauldron transmutes the material
        from.PlaySound(Utility.RandomList(0x22, 0x23));

        Charges -= newAmount;

        foreach (var component in Components)
        {
            component.InvalidateProperties();
        }

        return true;
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

    private sealed class InternalTarget : Target
    {
        private readonly CauldronOfTransmutation _addon;

        public InternalTarget(CauldronOfTransmutation addon) : base(-1, false, TargetFlags.None)
        {
            _addon = addon;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (_addon == null || !from.InRange(_addon.Location, 3))
            {
                from.SendLocalizedMessage(500295); // You are too far away to do that.
                return;
            }

            if (targeted is Item item)
            {
                _addon.TryTransmutate(from, item);
            }
        }
    }

    [SerializationGenerator(0, false)]
    public partial class InternalComponent : AddonComponent
    {
        public override bool ForceShowProperties => true;

        [Constructible]
        public InternalComponent(int id) : base(id)
        {
        }

        public override void AddNameProperty(IPropertyList list)
        {
            if (Addon is { Resource: not CraftResource.None } addon)
            {
                // ~1_RES~ Cauldron of Transmutation
                list.Add(1152600, $"#{CraftResources.GetLocalizationNumber(addon.Resource)}");
            }
            else
            {
                base.AddNameProperty(list);
            }
        }

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);

            if (Addon is not CauldronOfTransmutation cauldron || cauldron.Resource == CraftResource.None)
            {
                return;
            }

            var left = cauldron._expires > Core.Now ? (int)(cauldron._expires - Core.Now).TotalSeconds : 0;
            list.Add(1072517, left.ToString()); // Lifespan: ~1_val~ seconds

            var source = cauldron.Resource;
            var target = (CraftResource)((int)source + 1);

            // transmutes ~1_SOURCE~ to ~2_DEST~
            list.Add(
                1152630,
                $"#{CraftResources.GetLocalizationNumber(source)}\t#{CraftResources.GetLocalizationNumber(target)}"
            );
            list.Add(1152631, "3\t1"); // ratio ~1_INPUT~ to ~2_OUTPUT~
            list.Add(1060584, cauldron.Charges.ToString()); // uses remaining: ~1_val~
        }
    }
}

[SerializationGenerator(0, false)]
public partial class CauldronOfTransmutationDeed : BaseAddonDeed
{
    public const int DecayPeriod = 24;

    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private DateTime _expires;

    private TimerExecutionToken _decayToken;

    public override BaseAddon Addon => new CauldronOfTransmutation { Resource = Resource };

    [Constructible]
    public CauldronOfTransmutationDeed() : this(CraftResource.Iron)
    {
    }

    public CauldronOfTransmutationDeed(CraftResource resource)
    {
        Resource = resource;
        _expires = Core.Now + TimeSpan.FromHours(DecayPeriod);
        ScheduleDecay();
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

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();
        _decayToken.Cancel();
    }

    public override void AddNameProperty(IPropertyList list)
    {
        // ~1_RES~ Cauldron of Transmutation
        list.Add(1152600, $"#{CraftResources.GetLocalizationNumber(Resource)}");
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        var left = _expires > Core.Now ? (int)(_expires - Core.Now).TotalSeconds : 0;
        list.Add(1072517, left.ToString()); // Lifespan: ~1_val~ seconds
        list.Add(1152628); // cannot re-deed
        list.Add(1152629, CauldronOfTransmutation.DecayPeriod.ToString()); // lasts ~1_count~ hours
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
}
