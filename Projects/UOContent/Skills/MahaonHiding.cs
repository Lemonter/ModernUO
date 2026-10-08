using System;
using Server.Systems.MahaonProfessions;

namespace Server.SkillHandlers;

/// <summary>
///     Wraps the vanilla Hiding callback to toggle its own CombatOverride flag based on
///     profession — thief-touching professions ("могут использовать Hiding даже верхом на
///     лошади, в присутствии других, во время боя") bypass the normal "can't hide while
///     fighting or being watched" restriction; everyone else still gets the normal rules.
/// </summary>
public static class MahaonHiding
{
    public static void Initialize()
    {
        // Zero-delay so this installs after the vanilla Hiding.Initialize() has already run,
        // regardless of class load order — same trick used for ArmsLore/AnimalLore.
        Timer.DelayCall(TimeSpan.Zero, () => SkillInfo.Table[(int)SkillName.Hiding].Callback = OnUse);
    }

    private static TimeSpan OnUse(Mobile m)
    {
        var previous = Hiding.CombatOverride;
        Hiding.CombatOverride = ProfessionSystem.TouchesCategory(m, ProfessionCategory.Thief);

        try
        {
            return Hiding.OnUse(m);
        }
        finally
        {
            Hiding.CombatOverride = previous;
        }
    }
}
