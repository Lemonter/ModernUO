using System;
using ModernUO.Serialization;
using Server.Engines.BuffIcons;
using Server.Mobiles;
using Server.Spells.Mysticism;

namespace Server.Items;

/// <summary>
///     Портировано из ServUO. Индивидуальная чистка каждого проклятия заменена общим
///     CleansingWindsSpell.RemoveCurses: точечные RemoveEffects/TryEndEffect по школам в этом
///     репозитории отсутствуют не полностью, но сведены в один вызов.
/// </summary>
[SerializationGenerator(0, false)]
public partial class EnchantedApple : BaseMagicalFood
{
    [Constructible]
    public EnchantedApple() : base(0x2FD8)
    {
        Weight = 1.0;
        Hue = 0x488;
        Stackable = true;
    }

    public override MagicalFood FoodID => MagicalFood.EnchantedApple;

    public override TimeSpan Cooldown => TimeSpan.FromSeconds(30);

    // A tasty bite of the enchanted apple lifts all curses from your soul.
    public override int EatMessage => 1074846;

    public override bool Eat(Mobile from)
    {
        if (IsUnderInfluence(from, FoodID))
        {
            return false;
        }

        if (CoolingDown(from, FoodID))
        {
            from.SendLocalizedMessage(1151180); // You must wait a while before eating another enchanted apple.
            return false;
        }

        from.PlaySound(0xF6);
        from.PlaySound(0x1F7);
        from.FixedParticles(0x3709, 1, 30, 9963, 13, 3, EffectLayer.Head);

        var mfrom = new Entity(Serial.Zero, new Point3D(from.X, from.Y, from.Z - 10), from.Map);
        var mto = new Entity(Serial.Zero, new Point3D(from.X, from.Y, from.Z + 50), from.Map);
        Effects.SendMovingParticles(
            mfrom, mto, 0x2255, 1, 0, false, false, 13, 3, 9501, 1, 0, EffectLayer.Head, 0x100
        );

        var power = Math.Min(CleansingWindsSpell.RemoveCurses(from), 15);

        if (from is PlayerMobile pm)
        {
            pm.RemoveBuff(BuffIcon.MassCurse);
        }

        from.SendLocalizedMessage(EatMessage);

        StartInfluence(from, FoodID, Duration, TimeSpan.FromSeconds(30 + power));
        Consume();

        return true;
    }

    public static int GetTotalCurses(Mobile m) => 0;
}
