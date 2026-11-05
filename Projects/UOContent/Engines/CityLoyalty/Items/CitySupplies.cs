/// <summary>
///     Ported from ServUO (Scripts/Services/City Loyalty System/Items/BoxOfRopes.cs and GuardsmansRope.cs).
///
///     Substitutions:
///     <list type="bullet">
///         <item>
///             The rope cooldown is a static per-mobile table, exactly as in ServUO, but it is <b>not</b> serialized —
///             losing a 60 second cooldown across a restart is harmless, and ServUO's own save only pruned the table.
///         </item>
///         <item><c>DateTime.UtcNow</c> for the cooldown deadline became <c>Core.Now</c>.</item>
///     </list>
/// </summary>
using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;

namespace Server.Engines.CityLoyalty;

[SerializationGenerator(0, false)]
public partial class BoxOfRopes : Container
{
    private static Dictionary<Mobile, DateTime> _cooldown;

    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private City _city;

    [Constructible]
    public BoxOfRopes() : this(City.Britain)
    {
    }

    [Constructible]
    public BoxOfRopes(City city) : base(3650)
    {
        Hue = 1944;
        Movable = false;
        _city = city;

        if (CitySystem?.Captain != null)
        {
            CitySystem.Captain.Box = this;
        }
    }

    public CityLoyaltySystem CitySystem => CityLoyaltySystem.GetCityInstance(_city);

    public override int LabelNumber => 1152262; // a box of ropes

    public override void OnDoubleClick(Mobile from)
    {
        if (!CityLoyaltySystem.Enabled || !CityLoyaltySystem.IsSetup())
        {
            from.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 1019045, from.NetState);
            return;
        }

        if (!from.InRange(Location, 3))
        {
            from.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 1019045, from.NetState); // I can't reach that.
            return;
        }

        _cooldown ??= new Dictionary<Mobile, DateTime>();

        if (_cooldown.TryGetValue(from, out var next) && next >= Core.Now)
        {
            // You must wait a moment before taking another rope.
            from.SendLocalizedMessage(1152264);
            return;
        }

        from.AddToBackpack(new GuardsmansRope());

        // You take a rope from the chest. Use it to arrest rioters and subdued raiders.
        from.SendLocalizedMessage(1152263);

        _cooldown[from] = Core.Now + TimeSpan.FromSeconds(60);
    }

    private void AfterDeserialization()
    {
        if (CitySystem?.Captain != null)
        {
            CitySystem.Captain.Box = this;
        }
    }

    public static void Defrag()
    {
        if (_cooldown == null)
        {
            return;
        }

        var stale = new List<Mobile>();

        foreach (var (m, next) in _cooldown)
        {
            if (next < Core.Now)
            {
                stale.Add(m);
            }
        }

        foreach (var m in stale)
        {
            _cooldown.Remove(m);
        }
    }
}

[SerializationGenerator(0, false)]
public partial class GuardsmansRope : BaseDecayingItem
{
    [Constructible]
    public GuardsmansRope() : base(0x14F8)
    {
        Hue = 1944;
    }

    public override int LabelNumber => 1152261; // guardsman's rope
    public override int Lifespan => 3600;

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            // That must be in your backpack for you to use it.
            from.SendLocalizedMessage(1116249);
            return;
        }

        // What would you like to arrest?
        from.SendLocalizedMessage(1152244);

        from.BeginTarget(
            10,
            false,
            TargetFlags.None,
            (m, targeted) =>
            {
                if (targeted is not Raider raider)
                {
                    // You cannot arrest that.
                    m.SendLocalizedMessage(1152243);
                    return;
                }

                if (!raider.InRange(m.Location, 1))
                {
                    // You cannot reach that.
                    m.SendLocalizedMessage(1152242);
                    return;
                }

                if (raider.TryArrest(m))
                {
                    Delete();
                }
            }
        );
    }
}
