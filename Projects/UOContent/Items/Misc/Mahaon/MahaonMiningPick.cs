using System;
using ModernUO.Serialization;
using Server.Systems.MahaonMining;
using Server.Targeting;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class MahaonMiningPick : Item
{
    [Constructible]
    public MahaonMiningPick() : base(0x0E86)
    {
        Weight = 5.0;
        Name = "горная кирка";
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendMessage("Это должно быть у тебя в рюкзаке, чтобы использовать.");
            return;
        }

        from.SendMessage("Укажи скальную стену чтобы копать, или склон горы, чтобы начать новую шахту.");
        from.Target = new MiningDigTarget();
    }
}

public class MiningDigTarget : Target
{
    // One click, then the pick keeps swinging on its own — real Mining skill decides how
    // many times, same as if you'd manually re-used the tool that many times in a row.
    private const double SwingDelaySeconds = 1.5;

    public MiningDigTarget() : base(2, true, TargetFlags.None)
    {
    }

    protected override void OnTarget(Mobile from, object targeted)
    {
        var map = from.Map;
        if (map == null)
        {
            return;
        }

        // Digging an existing rock wall (inside a mine) expands the shaft.
        if (targeted is MineRockWall wall)
        {
            if (!from.InRange(wall.GetWorldLocation(), 2))
            {
                from.SendMessage("Слишком далеко.");
                return;
            }

            var miningSkill = from.Skills[SkillName.Mining].Value;
            var swings = Math.Max(1, (int)(miningSkill / 10.0));

            DoWallSwings(from, map, wall.Location, swings);
            return;
        }

        if (targeted is not IPoint3D p)
        {
            from.SendMessage("Здесь копать нельзя.");
            return;
        }

        var loc = new Point3D(p.X, p.Y, p.Z);

        if (!from.InRange(loc, 2))
        {
            from.SendMessage("Слишком далеко.");
            return;
        }

        if (!IsNearMountain(map, loc))
        {
            from.SendMessage("Чтобы начать шахту здесь, нужно быть рядом со склоном горы или скальной стеной.");
            return;
        }

        if (MineComplexSystem.HasEntranceAt(loc, map))
        {
            from.SendMessage("Здесь уже есть шахта — встань на вход, чтобы зайти внутрь.");
            return;
        }

        var surfaceSkill = from.Skills[SkillName.Mining].Value;
        var surfaceSwings = Math.Max(1, (int)(surfaceSkill / 10.0));

        DoSurfaceSwings(from, map, loc, surfaceSwings, true);
    }

    // -- Digging into an existing rock wall, repeated ------------------------------------

    private static void DoWallSwings(Mobile from, Map map, Point3D anchorLoc, int swingsRemaining)
    {
        if (!CanContinue(from, map, anchorLoc, swingsRemaining))
        {
            return;
        }

        // The exact wall from the first click may already be gone (dug through on an
        // earlier swing) — find whatever rock wall is still standing closest to where the
        // player's been digging and keep going into that.
        MineRockWall nextWall = null;
        var bestDistance = int.MaxValue;

        foreach (var item in map.GetItemsInRange<MineRockWall>(anchorLoc, 2))
        {
            var d = Math.Max(Math.Abs(item.X - anchorLoc.X), Math.Abs(item.Y - anchorLoc.Y));
            if (d < bestDistance)
            {
                bestDistance = d;
                nextWall = item;
            }
        }

        if (nextWall == null)
        {
            from.SendMessage("Больше нечего копать поблизости.");
            return;
        }

        from.Direction = from.GetDirectionTo(nextWall);
        from.Animate(11, 5, 1, true, false, 0);

        if (!MineComplexSystem.TryDigWall(from, nextWall, map, out var ore))
        {
            from.SendMessage("Ты копаешь некоторое время, но порода не поддаётся.");
        }
        else if (ore != null)
        {
            if (from.Backpack?.TryDropItem(from, ore, false) != true)
            {
                ore.MoveToWorld(from.Location, map);
            }

            from.SendMessage(0x59, $"Стена поддаётся. Ты добываешь: {ore.Amount} x {OreNameRu(ore)}.");
        }

        Timer.DelayCall(
            TimeSpan.FromSeconds(SwingDelaySeconds),
            () => DoWallSwings(from, map, anchorLoc, swingsRemaining - 1)
        );
    }

    // -- Digging a natural mountainside, repeated -----------------------------------------

    private static void DoSurfaceSwings(Mobile from, Map map, Point3D loc, int swingsRemaining, bool allowMineRoll)
    {
        if (!CanContinue(from, map, loc, swingsRemaining))
        {
            return;
        }

        from.Direction = from.GetDirectionTo(loc);
        from.Animate(11, 5, 1, true, false, 0);

        var miningSkill = from.Skills[SkillName.Mining].Value;

        if (!from.CheckSkill(SkillName.Mining, 0.0, 100.0))
        {
            from.SendMessage("Ты копаешь некоторое время, но порода не поддаётся.");
        }
        else
        {
            var oreType = MahaonResourceTiers.PickOre(miningSkill);
            var directOre = (Item)Activator.CreateInstance(oreType, 1);

            if (from.Backpack?.TryDropItem(from, directOre, false) != true)
            {
                directOre.MoveToWorld(from.Location, map);
            }

            from.SendMessage(0x59, $"Ты выкапываешь: {directOre.Amount} x {OreNameRu(directOre)}.");

            // Only worth rolling for a fresh mine entrance once per target — no reason to
            // re-check it on every single swing of the same dig.
            if (allowMineRoll && Utility.RandomDouble() < 0.15)
            {
                MineComplexSystem.CreateMine(from, loc, from.Direction, map);
                allowMineRoll = false;
            }
        }

        Timer.DelayCall(
            TimeSpan.FromSeconds(SwingDelaySeconds),
            () => DoSurfaceSwings(from, map, loc, swingsRemaining - 1, allowMineRoll)
        );
    }

    private static bool CanContinue(Mobile from, Map map, Point3D anchorLoc, int swingsRemaining)
    {
        return swingsRemaining > 0 && !from.Deleted && from.Alive && from.Map == map && from.InRange(anchorLoc, 3);
    }

    public static bool IsNearMountain(Map map, Point3D loc)
    {
        for (var dx = -2; dx <= 2; dx++)
        {
            for (var dy = -2; dy <= 2; dy++)
            {
                var lt = map.Tiles.GetLandTile(loc.X + dx, loc.Y + dy);
                var flags = TileData.LandTable[lt.ID & TileData.MaxLandValue].Flags;

                if ((flags & TileFlag.Impassable) != 0)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static string OreNameRu(Item ore) => ore switch
    {
        IronOre       => "железная руда",
        DullCopperOre => "тусклая медная руда",
        ShadowIronOre => "теневое железо",
        CopperOre     => "медная руда",
        BronzeOre     => "бронзовая руда",
        GoldOre       => "золотая руда",
        AgapiteOre    => "агапитовая руда",
        VeriteOre     => "веритовая руда",
        ValoriteOre   => "валоритовая руда",
        _             => ore.GetType().Name
    };
}
