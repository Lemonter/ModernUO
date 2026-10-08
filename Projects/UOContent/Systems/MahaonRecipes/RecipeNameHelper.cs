using Server.Engines.Craft;

namespace Server.Systems.MahaonRecipes;

/// <summary>
///     Recipe.TextDefinition.ToString() returns a raw "#12345" placeholder when the name
///     is cliloc-based — that syntax is only meaningful in specific client-side contexts
///     (like certain gump string slots), not in a plain Item.Name or SendMessage string,
///     where it just shows as garbage. This resolves the real text server-side instead,
///     using the same Cliloc table ModernUO already loads for its own purposes.
/// </summary>
public static class RecipeNameHelper
{
    public static string GetName(Recipe recipe)
    {
        var td = recipe.TextDefinition;

        if (td.Number > 0)
        {
            var text = Localization.GetText(td.Number);
            if (!string.IsNullOrEmpty(text))
            {
                return text;
            }
        }

        return !string.IsNullOrEmpty(td.String) ? td.String : "неизвестный предмет";
    }
}
