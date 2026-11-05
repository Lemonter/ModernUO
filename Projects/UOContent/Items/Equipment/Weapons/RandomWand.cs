using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Weapons/RandomWand.cs).
/// </summary>
public class RandomWand
{
    public static BaseWand CreateWand()
    {
        return CreateRandomWand();
    }

    public static BaseWand CreateRandomWand()
    {
        return Loot.RandomWand();
    }
}
