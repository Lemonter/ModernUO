namespace Server.Items;

/// <summary>
///     The only thing ServUO's <c>BlackthornBaseAddon</c> provided the nine ForsakenFoes decoration addons was
///     <c>AddComplexComponent</c>, which adds a component with a hue, a light source, an optional name and a stack
///     amount. An abstract addon base class would force every one of them to carry serialization they do not use, so
///     the helper is a plain static class they import with <c>using static</c>. The addons are all YAAAG output: the
///     name and amount arguments are always empty and 1, so the two-argument overload ServUO kept is not ported.
/// </summary>
public static class ForsakenFoesAddonHelper
{
    public static void AddComplexComponent(
        BaseAddon addon, int item, int xOffset, int yOffset, int zOffset, int hue, int lightSource
    )
    {
        var component = new AddonComponent(item);

        if (hue != 0)
        {
            component.Hue = hue;
        }

        if (lightSource != -1)
        {
            component.Light = (LightType)lightSource;
        }

        addon.AddComponent(component, xOffset, yOffset, zOffset);
    }
}
