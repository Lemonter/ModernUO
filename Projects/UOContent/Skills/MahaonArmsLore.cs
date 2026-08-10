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
