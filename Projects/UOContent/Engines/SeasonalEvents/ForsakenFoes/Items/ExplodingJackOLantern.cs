using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Items;

/// <summary>Ported from ServUO (Scripts/Services/Seasonal Events/ForsakenFoes/Items/ExplodingJackOLantern.cs).</summary>
[SerializationGenerator(0, false)]
public partial class ExplodingJackOLantern : Item
{
    public override int LabelNumber => 1159220; // Exploding Jack o' Lantern

    [Constructible]
    public ExplodingJackOLantern() : base(0xA407)
    {
        Weight = 1.0;
        LootType = LootType.Blessed;
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (from.Mounted)
        {
            from.SendLocalizedMessage(1061130); // You can't do that while riding a mount.
            return;
        }

        from.SendLocalizedMessage(1113280); // Which target do you wish to throw this at?
        from.Target = new ThrowTarget(from);
    }

    private class ThrowTarget : Targeting.Target
    {
        public ThrowTarget(Mobile from) : base(12, true, Targeting.TargetFlags.None)
        {
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted is not Mobile m)
            {
                return;
            }

            if (from == m)
            {
                from.SendLocalizedMessage(501588); // Verbal taunts might be more effective!
                return;
            }

            Effects.SendMovingEffect(from, m, 0xA407, 10, 0, false, false, 0, 0);
            from.SendLocalizedMessage(1159219); // You hit your target!
            m.SendLocalizedMessage(1159218);    // You have just been hit by a Jack o' Lantern!!!
            m.PlaySound(519);
        }
    }
}
