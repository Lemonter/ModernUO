using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Systems.MahaonSeasons;
using Server.Targeting;

namespace Server.Items;

/// <summary>Not actually needed to be equipped or held specially — the mechanic is just
/// "approach a real tree and target it" to take a sapling that'll grow into the same
/// species.</summary>
[SerializationGenerator(0, false)]
public partial class MahaonPruningKnife : Item
{
    [Constructible]
    public MahaonPruningKnife() : base(0x0F52)
    {
        Weight = 1.0;
        Name = "секатор";
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendMessage("Это должно быть у тебя в рюкзаке, чтобы использовать.");
            return;
        }

        from.SendMessage("Укажи дерево, с которого возьмёшь саженец.");
        from.Target = new SaplingGatherTarget();
    }
}

public class SaplingGatherTarget : Target
{
    // Per-location cooldown so the same wild tree can't be stripped repeatedly.
    private static readonly Dictionary<(int x, int y, Map map), DateTime> LastGathered = new();
    private static readonly TimeSpan GatherCooldown = TimeSpan.FromMinutes(5);

    public SaplingGatherTarget() : base(2, true, TargetFlags.None)
    {
    }

    protected override void OnTarget(Mobile from, object targeted)
    {
        if (targeted is not IPoint3D p)
        {
            from.SendMessage("Это не дерево.");
            return;
        }

        var map = from.Map;
        if (map == null)
        {
            return;
        }

        var loc = new Point3D(p.X, p.Y, p.Z);

        if (!TryMatchTreeAt(map, loc, out var species))
        {
            from.SendMessage("С этого дерева саженец не взять.");
            return;
        }

        var key = (loc.X, loc.Y, map);
        if (LastGathered.TryGetValue(key, out var last) && Core.Now - last < GatherCooldown)
        {
            from.SendMessage("Это дерево недавно уже трогали — дай ему время восстановиться.");
            return;
        }

        LastGathered[key] = Core.Now;

        var sapling = new MahaonSapling(species);

        if (from.Backpack?.TryDropItem(from, sapling, false) != true)
        {
            sapling.MoveToWorld(from.Location, map);
        }

        from.SendMessage(0x59, "Ты срезаешь здоровый саженец с дерева.");
    }

    private static bool TryMatchTreeAt(Map map, Point3D loc, out MahaonTreeSpecies species)
    {
        // Our own planted trees are real Items — check those first.
        foreach (var item in map.GetItemsInRange<MahaonTree>(loc, 0))
        {
            if (item.Location == loc)
            {
                species = item.Species;
                return true;
            }
        }

        // Otherwise it's a wild vanilla tree static — match by graphic.
        foreach (var tile in map.Tiles.GetStaticTiles(loc.X, loc.Y))
        {
            if (MahaonTreeSpeciesTable.TryMatchTrunk(tile.ID, out species))
            {
                return true;
            }
        }

        species = default;
        return false;
    }
}
