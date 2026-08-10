using System;
using Server.Gumps;
using Server.Targeting;

namespace Server.SkillHandlers;

public static class MahaonItemIdentification
{
    public static void Initialize()
    {
        Timer.DelayCall(TimeSpan.Zero, () => SkillInfo.Table[(int)SkillName.ItemID].Callback = OnUse);
    }

    public static TimeSpan OnUse(Mobile from)
    {
        from.SendMessage("Укажи предмет, статику или тайл земли, чтобы посмотреть его данные.");
        from.Target = new InternalTarget();

        return TimeSpan.FromSeconds(1.0);
    }

    private class InternalTarget : Target
    {
        public InternalTarget() : base(10, true, TargetFlags.None) => AllowNonlocal = true;

        protected override void OnTarget(Mobile from, object o)
        {
            from.SendGump(new MahaonDebugItemGump(o));
        }
    }
}
