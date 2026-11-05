using System;
using ModernUO.Serialization;
using Server.Engines.BuffIcons;
using Server.Mobiles;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class GrapesOfWrath : BaseMagicalFood, ICommodity
{
    [Constructible]
    public GrapesOfWrath() : base(0x2FD7)
    {
        Weight = 1.0;
        Hue = 0x482;
        Stackable = true;
    }

    public int DescriptionNumber => LabelNumber;

    public bool IsDeedable => true;

    public override MagicalFood FoodID => MagicalFood.GrapesOfWrath;

    public override TimeSpan Cooldown => TimeSpan.FromMinutes(2);

    public override TimeSpan Duration => TimeSpan.FromSeconds(20);

    // The grapes of wrath invigorate you for a short time, allowing you to deal extra damage.
    public override int EatMessage => 1074847;

    public override bool Eat(Mobile from)
    {
        if (!base.Eat(from))
        {
            return false;
        }

        if (from is PlayerMobile pm)
        {
            pm.AddBuff(new BuffInfo(BuffIcon.GrapesOfWrath, 1032247, 1153762, Duration, "15\t35"));
        }

        return true;
    }
}
