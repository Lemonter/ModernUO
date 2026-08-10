using Server.Commands;
using Server.Mobiles;

namespace Server.Systems.MahaonBots;

public static class BotSpawnCommands
{
    public static void Configure()
    {
        CommandSystem.Register("GenBots", AccessLevel.GameMaster, GenBots_OnCommand);
    }

    [Usage("GenBots <count>")]
    [Description("Disabled — bots now only spawn from a MahaonBotBeacon statue.")]
    private static void GenBots_OnCommand(CommandEventArgs e)
    {
        e.Mobile.SendMessage(0x22, "Отключено — теперь боты появляются только от статуи-маяка ([AddLem).");
    }
}
