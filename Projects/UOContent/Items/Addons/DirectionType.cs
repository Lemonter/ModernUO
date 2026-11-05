namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Internal/ItemInterfaces.cs).
///     ServUO defines this enum alongside IFlipable/IQuality in Items/Internal; only the enum is needed by the
///     fireplace and table addons ported here, so it is declared once in this folder.
/// </summary>
public enum DirectionType
{
    None = 0,
    South = 1,
    East = 2,
    North = 3,
    West = 4
}
