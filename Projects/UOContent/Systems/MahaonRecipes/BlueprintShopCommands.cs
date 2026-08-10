using Server.Commands;
using Server.Gumps;

namespace Server.Systems.MahaonRecipes;

public static class BlueprintShopCommands
{
    public static void Configure()
    {
        CommandSystem.Register("BlueprintShop", AccessLevel.Player, BlueprintShop_OnCommand);
    }

    [Usage("BlueprintShop")]
    [Description("Opens the universal crafting blueprint shop.")]
    private static void BlueprintShop_OnCommand(CommandEventArgs e)
    {
        e.Mobile.SendGump(new MahaonBlueprintCategoryGump(e.Mobile));
    }
}
