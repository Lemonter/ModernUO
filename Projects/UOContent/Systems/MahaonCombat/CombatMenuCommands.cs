using Server.Commands;
using Server.Gumps;

namespace Server.Systems.MahaonCombat;

public static class CombatMenuCommands
{
    public static void Configure()
    {
        CommandSystem.Register("CombatMenu", AccessLevel.Player, CombatMenu_OnCommand);
    }

    [Usage("CombatMenu")]
    [Description("Opens the Mahaon combat stance / hit-location menu.")]
    private static void CombatMenu_OnCommand(CommandEventArgs e)
    {
        e.Mobile.SendGump(new MahaonCombatMenuGump(e.Mobile));
    }
}
