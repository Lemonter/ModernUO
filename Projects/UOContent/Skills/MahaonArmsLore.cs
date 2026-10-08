using System;
using Server.Gumps;
using Server.Items;
using Server.Targeting;

namespace Server.SkillHandlers;

public static class MahaonArmsLore
{
    public static void Initialize()
    {
        // The vanilla ArmsLore skill handler sets this same callback in its own
        // Initialize() — same lifecycle stage, so which one wins isn't guaranteed. A
        // zero-delay timer runs strictly after every Initialize() call has finished,
        // so this always ends up set last regardless of class load order.
        Timer.DelayCall(TimeSpan.Zero, () => SkillInfo.Table[(int)SkillName.ArmsLore].Callback = OnUse);
    }

    /// <summary>
    ///     Mahaon: knowing your gear means it lasts longer. Rolled at every point where a
    ///     weapon or a piece of armour is about to lose durability (BaseWeapon.OnHit and
    ///     BaseArmor.OnHit) - on success the wear simply does not happen this time.
    ///
    ///     Scales straight off the skill: nothing at 0, MaxWearReduction at 100. Applied to
    ///     the owner of the item, so a bot or a monster benefits from its own Arms Lore
    ///     exactly the way a player does.
    /// </summary>
    private const double MaxWearReduction = 0.50;

    public static double GetWearReduction(Mobile owner)
    {
        var skill = owner?.Skills.ArmsLore.Value ?? 0.0;

        return Math.Clamp(skill / 100.0, 0.0, 1.0) * MaxWearReduction;
    }

    /// <summary>True when this instance of wear should be skipped entirely.</summary>
    public static bool TryPreventWear(Mobile owner)
    {
        var reduction = GetWearReduction(owner);

        if (reduction <= 0.0)
        {
            return false;
        }

        if (Utility.RandomDouble() >= reduction)
        {
            return false;
        }

        // Using the knowledge is how it is practised.
        owner?.CheckSkill(SkillName.ArmsLore, 0, 100);

        return true;
    }

    public static TimeSpan OnUse(Mobile m)
    {
        m.Target = new InternalTarget();
        m.SendMessage("Укажи оружие или броню, о которых хочешь узнать.");
        return TimeSpan.FromSeconds(1.0);
    }

    private class InternalTarget : Target
    {
        public InternalTarget() : base(2, false, TargetFlags.None)
        {
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted is not (BaseWeapon or BaseArmor))
            {
                from.SendMessage("Это не оружие и не броня.");
                return;
            }

            var item = (Item)targeted;

            if (!from.CheckTargetSkill(SkillName.ArmsLore, item, 0, 100))
            {
                from.SendMessage("Ты не уверен, что можешь сказать об этом что-то путное.");
                return;
            }

            from.SendGump(new MahaonArmsLoreGump(item));
        }
    }
}
