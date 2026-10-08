using System;
using Server.Gumps;
using Server.Mobiles;
using Server.Targeting;

namespace Server.SkillHandlers;

public static class MahaonAnimalLore
{
    public static void Initialize()
    {
        Timer.DelayCall(TimeSpan.Zero, () => SkillInfo.Table[(int)SkillName.AnimalLore].Callback = OnUse);
    }

    public static TimeSpan OnUse(Mobile m)
    {
        m.Target = new InternalTarget();
        m.SendMessage("Укажи животное, о котором хочешь узнать.");
        return TimeSpan.FromSeconds(1.0);
    }

    private class InternalTarget : Target
    {
        public InternalTarget() : base(10, false, TargetFlags.None)
        {
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted is not BaseCreature creature)
            {
                from.SendMessage("Это не животное.");
                return;
            }

            if (!from.CheckTargetSkill(SkillName.AnimalLore, creature, 0, 100))
            {
                from.SendMessage("Ты не можешь понять природу этого существа.");
                return;
            }

            from.SendGump(new MahaonAnimalLoreGump(creature));
        }
    }
}
