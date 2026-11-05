using System;
using ModernUO.Serialization;
using Server.Multis;
using Server.Spells;
using Server.Targeting;

namespace Server.Items;

/// <summary>
///     Ported from ServUO's Covetous void pool (Items/TemporaryForge.cs) — a portable forge that lasts
///     four hours and a deed that lasts a day, both bought with void pool points.
///
///     The deed is the only part that had to be rewritten rather than transliterated. ServUO lets it be
///     placed outside a house (2-5 tiles away, LOS, target tile passable) via
///     <c>TreasureMap.ValidateLocation</c>; neither that helper nor a non-house addon path exists in this
///     codebase's BaseAddonDeed, which only accepts "in a house you own". So: inside a house the normal
///     placement runs, outside it the deed checks the same 2-5 tile band and that the spot can hold a
///     mobile, then drops the addon there.
///
///     Decay is a single one-shot timer armed for the remaining lifespan (cancelled in OnAfterDelete),
///     not the original 10-second poll.
/// </summary>
[SerializationGenerator(0, false)]
public partial class TemporaryForge : BaseAddon
{
    public const int DecayPeriod = 4;

    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private DateTime _expires;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Mobile _owner;

    private TimerExecutionToken _decayToken;

    [Constructible]
    public TemporaryForge()
    {
        AddComponent(new InternalComponent(), 0, 0, 0);

        _expires = Core.Now + TimeSpan.FromHours(DecayPeriod);
        ScheduleDecay();
    }

    public override void OnChop(Mobile from)
    {
        if (BaseHouse.FindHouseAt(this) == null && Owner == from)
        {
            Effects.PlaySound(GetWorldLocation(), Map, 0x3B3);
            from.SendLocalizedMessage(500461); // You destroy the item.
            Delete();
            return;
        }

        base.OnChop(from);
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

    [SerializationGenerator(0, false)]
    public partial class InternalComponent : ForgeComponent
    {
        public override bool ForceShowProperties => true;
        public override int LabelNumber => 1152601; // Temporary forge

        [Constructible]
        public InternalComponent() : base(0xFB1)
        {
        }

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);

            if (Addon is not TemporaryForge forge)
            {
                return;
            }

            var left = forge._expires > Core.Now ? (int)(forge._expires - Core.Now).TotalSeconds : 0;
            list.Add(1072517, left.ToString()); // Lifespan: ~1_val~ seconds
        }
    }
}

[SerializationGenerator(0, false)]
public partial class TemporaryForgeDeed : BaseAddonDeed
{
    public const int DecayPeriod = 24;

    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private DateTime _expires;

    private TimerExecutionToken _decayToken;

    public override BaseAddon Addon => new TemporaryForge();

    [Constructible]
    public TemporaryForgeDeed()
    {
        _expires = Core.Now + TimeSpan.FromHours(DecayPeriod);
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
        list.Add(1152601); // Temporary Forge
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        var left = _expires > Core.Now ? (int)(_expires - Core.Now).TotalSeconds : 0;
        list.Add(1072517, left.ToString()); // Lifespan: ~1_val~ seconds
        list.Add(1152627); // no house required
        list.Add(1152628); // cannot re-deed
        list.Add(1152629, TemporaryForge.DecayPeriod.ToString()); // lasts ~1_count~ hours
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            return;
        }

        if (BaseHouse.FindHouseAt(from) != null)
        {
            base.OnDoubleClick(from);
            return;
        }

        from.Target = new FreePlacementTarget(this);
    }

    private void PlaceGranted(Mobile from, Point3D p, Map map)
    {
        var addon = Addon;
        addon.MoveToWorld(p, map);

        if (addon is TemporaryForge forge)
        {
            forge.Owner = from;
        }

        Delete();
    }

    private sealed class FreePlacementTarget : Target
    {
        private readonly TemporaryForgeDeed _deed;

        public FreePlacementTarget(TemporaryForgeDeed deed) : base(10, true, TargetFlags.None)
        {
            _deed = deed;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted is not IPoint3D point || from.Map == null || _deed.Deleted)
            {
                return;
            }

            if (!_deed.IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
                return;
            }

            var p = new Point3D(point);
            var distance = (int)from.GetDistanceToSqrt(p);

            if (distance < 2 || distance > 5)
            {
                // You must stand between 2 and 5 tiles away from the targeted location to attempt to build this.
                from.SendLocalizedMessage(1152736);
                return;
            }

            if (!from.InLOS(p))
            {
                from.SendLocalizedMessage(500237); // Target cannot be seen.
                return;
            }

            SpellHelper.GetSurfaceTop(ref point);

            var surface = new Point3D(point);

            if (!from.Map.CanSpawnMobile(surface))
            {
                // The targeted location has at least one impassable tile adjacent to the structure.
                from.SendLocalizedMessage(1152735);
                return;
            }

            _deed.PlaceGranted(from, surface, from.Map);
        }
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
