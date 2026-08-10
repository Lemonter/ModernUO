using Server.Commands;
using Server.Items;
using Server.Systems.MahaonBots;

namespace Server.Systems.MahaonRaids;

/// <summary>
///     Scatters tiered treasure chests around the known dungeon entrances (see
///     BotParty.DungeonTarget.Known). The 5-level loot-quality system the player remembered
///     already exists natively as BaseTreasureChest.TreasureLevel (it actually has 6 levels)
///     — no need to reinvent that, just place them somewhere. Chests deeper "in" a dungeon
///     aren't modeled (we don't have real interior layouts) — everything spawns in a ring
///     around the entrance point instead, with level loosely tied to distance from it.
/// </summary>
public static class DungeonChestSeeder
{
    private const int ChestsPerDungeon = 8;
    private const int MinRadius = 10;
    private const int MaxRadius = 40;

    public static void Configure()
    {
        CommandSystem.Register("SeedDungeonChests", AccessLevel.GameMaster, SeedDungeonChests_OnCommand);
    }

    [Usage("SeedDungeonChests")]
    [Description("Scatters tiered treasure chests around every known dungeon entrance.")]
    private static void SeedDungeonChests_OnCommand(CommandEventArgs e)
    {
        var placed = 0;

        foreach (var dungeon in DungeonTarget.Known)
        {
            placed += SeedDungeon(dungeon);
        }

        e.Mobile.SendMessage(0x59, $"Расставлено сундуков: {placed}.");
    }

    private static int SeedDungeon(DungeonTarget dungeon)
    {
        var placedCount = 0;

        for (var i = 0; i < ChestsPerDungeon; i++)
        {
            var radius = Utility.RandomMinMax(MinRadius, MaxRadius);
            var angle = Utility.RandomDouble() * System.Math.PI * 2;

            var x = dungeon.Entrance.X + (int)(System.Math.Cos(angle) * radius);
            var y = dungeon.Entrance.Y + (int)(System.Math.Sin(angle) * radius);
            var z = dungeon.Map.GetAverageZ(x, y);
            var candidate = new Point3D(x, y, z);

            if (!dungeon.Map.CanFit(candidate.X, candidate.Y, candidate.Z, 16))
            {
                continue;
            }

            // Farther from the entrance = deeper in (loosely) = better loot. Pure flavor
            // math, not tied to any real interior layout.
            var depthFactor = (radius - MinRadius) / (double)(MaxRadius - MinRadius);
            var level = (BaseTreasureChest.TreasureLevel)System.Math.Clamp(
                (int)(depthFactor * 5),
                0,
                5
            );

            var chest = new BaseTreasureChest(0x9AB, level);
            CurrencyHelper.ConvertVanillaGold(chest);
            chest.MoveToWorld(candidate, dungeon.Map);
            placedCount++;
        }

        return placedCount;
    }
}
