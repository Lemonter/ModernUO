using System;
using System.Collections.Generic;
using Server.Gumps;
using Server.Items;
using Server.Systems.MahaonCombat;
using Server.Systems.MahaonMetals;

namespace Server.Systems.MahaonMining;

/// <summary>
///     Drives real per-swing mining — animation, a real skill check, a small yield, repeat —
///     for both natural mountainside tiles and MineRockWall. Keeps working automatically
///     until the spot runs dry or something interrupts it, same as real classic UO mining.
/// </summary>
public static class MahaonMiningSwings
{
    private static readonly TimeSpan SwingDelay = TimeSpan.FromSeconds(1.6);

    // How much ore a fresh natural-ground spot holds before it's tapped out, and how long
    // it takes to respawn afterward — same idea as real UO ore veins, not an instant
    // refresh the moment you exhaust it.
    private static readonly Dictionary<(Map map, int x, int y), int> SurfaceReserve = new();
    private static readonly Dictionary<(Map map, int x, int y), DateTime> DepletedUntil = new();

    private const int SurfaceReserveMin = 8;
    private const int SurfaceReserveMax = 20;
    private static readonly TimeSpan RespawnDelay = TimeSpan.FromMinutes(10);
    private const double CoalChance = 0.15;

    public static void StartWallSwings(Mobile from, MineRockWall wall, Item tool)
    {
        if (!from.CanBeginAction(typeof(MahaonMiningSwings)))
        {
            return; // already mid-swing-chain
        }

        from.BeginAction(typeof(MahaonMiningSwings));
        DoWallSwing(from, wall, tool);
    }

    private static void DoWallSwing(Mobile from, MineRockWall wall, Item tool)
    {
        if (!CanContinue(from, wall.GetWorldLocation()) || wall.Deleted)
        {
            from.EndAction(typeof(MahaonMiningSwings));
            return;
        }

        from.Direction = from.GetDirectionTo(wall.GetWorldLocation());
        from.Animate(11, 5, 1, true, false, 0);
        from.PlaySound(Utility.RandomList(0x125, 0x126));

        var wallMap = wall.Map;
        var wasVein = wall.IsVein;

        if (!MineComplexSystem.TryDigWall(from, wall, wallMap, out var ore))
        {
            from.SendMessage("Ты копаешь некоторое время, но порода не поддаётся.");
        }
        else if (ore != null)
        {
            ApplyToolBonus(tool, ore);

            // MahaonOre (the everyday drop) uses the real 24-metal tier; a rich vein can
            // rarely hand back a genuine vanilla BaseOre instead (MineComplexSystem's
            // CreateVeinResource fallback) — TierForVanillaOre covers that case too, so
            // gathering specialization grows from either drop type.
            if (ore is MahaonOre mahaonOre)
            {
                GatheringSpecializationSystem.OnOreMined(from, MahaonMetalTable.Get(mahaonOre.Metal).Tier, ore);
            }
            else if (ore is BaseOre baseOre)
            {
                GatheringSpecializationSystem.OnOreMined(from, GatheringSpecializationSystem.TierForVanillaOre(baseOre.Resource), ore);
            }

            if (from.Backpack?.TryDropItem(from, ore, false) != true)
            {
                ore.MoveToWorld(from.Location, wallMap);
            }

            var label = wasVein ? "Богатая жила поддаётся!" : "Ты добываешь";
            from.SendMessage(0x59, $"{label}: {ore.Amount} x {ore.Name}.");

            if (Utility.RandomDouble() < CoalChance)
            {
                var coal = new MahaonCoal();
                if (from.Backpack?.TryDropItem(from, coal, false) != true)
                {
                    coal.MoveToWorld(from.Location, wallMap);
                }

                from.SendMessage(0x59, "Заодно попался кусок угля.");
            }
        }

        // The wall may have just broken through to floor — if so, there's nothing left at
        // this exact spot to keep swinging at.
        if (wall.Deleted)
        {
            from.EndAction(typeof(MahaonMiningSwings));
            from.SendMessage(0x59, "Стена поддаётся и открывает проход дальше.");
            return;
        }

        Timer.DelayCall(SwingDelay, () => DoWallSwing(from, wall, tool));
    }

    public static void StartSurfaceSwings(
        Mobile from, Point3D targetLoc, Map map, Item tool, bool allowMineOffer = true, bool isInsideMine = false
    )
    {
        var key = (map, targetLoc.X, targetLoc.Y);
        if (DepletedUntil.TryGetValue(key, out var until) && Core.Now < until)
        {
            from.SendMessage("Здесь недавно всё выкопали — жила ещё не восстановилась.");
            return;
        }

        if (!from.CanBeginAction(typeof(MahaonMiningSwings)))
        {
            return;
        }

        from.BeginAction(typeof(MahaonMiningSwings));

        // Both captured right now, at the moment digging actually starts — not read again
        // later when the "start a mine?" gump gets answered, by which point the player may
        // have walked off or turned to face something else entirely.
        var standingLoc = from.Location;
        var facing = from.Direction & Direction.Mask;

        DoSurfaceSwing(from, targetLoc, standingLoc, facing, map, tool, allowMineOffer, isInsideMine);
    }

    private static void DoSurfaceSwing(
        Mobile from, Point3D targetLoc, Point3D standingLoc, Direction facing, Map map, Item tool,
        bool allowMineOffer, bool isInsideMine
    )
    {
        if (!CanContinue(from, targetLoc))
        {
            from.EndAction(typeof(MahaonMiningSwings));
            return;
        }

        from.Direction = from.GetDirectionTo(targetLoc);
        from.Animate(11, 5, 1, true, false, 0);
        from.PlaySound(Utility.RandomList(0x125, 0x126));

        var key = (map, targetLoc.X, targetLoc.Y);
        if (!SurfaceReserve.TryGetValue(key, out var reserve))
        {
            reserve = Utility.RandomMinMax(SurfaceReserveMin, SurfaceReserveMax);
        }

        var miningSkill = from.Skills[SkillName.Mining].Value;

        if (from.CheckSkill(SkillName.Mining, 0.0, 100.0))
        {
            var metal = Systems.MahaonMetals.MahaonOreGenerator.PickMetal(miningSkill);
            var ore = new MahaonOre(metal, 1);
            // Тул-бонус (кирка своего металла добывает быстрее) — не перенесён на новую
            // систему в этом заходе, требует отдельной привязки металла к самой кирке
            // (ковка кирок из новых металлов ещё не построена).

            GatheringSpecializationSystem.OnOreMined(from, MahaonMetalTable.Get(metal).Tier, ore);

            if (from.Backpack?.TryDropItem(from, ore, false) != true)
            {
                ore.MoveToWorld(from.Location, map);
            }

            // Mahaon: было ore.GetType().Name — MahaonOre хранит металл полем (_metal),
            // не отдельным классом на тип, так что это ВСЕГДА печатало буквально
            // "MahaonOre" независимо от того, какой металл реально выкопан (сам предмет
            // при этом клался в рюкзак с правильным именем/цветом — несовпадение было
            // только в этом сообщении). ore.Name уже верно выставлено в конструкторе
            // MahaonOre на реальное русское название металла.
            from.SendMessage(0x59, $"Ты выкапываешь: {ore.Amount} x {ore.Name}.");

            if (Utility.RandomDouble() < CoalChance)
            {
                var coal = new MahaonCoal();
                if (from.Backpack?.TryDropItem(from, coal, false) != true)
                {
                    coal.MoveToWorld(from.Location, map);
                }

                from.SendMessage(0x59, "Заодно попался кусок угля.");
            }

            reserve--;
        }
        else
        {
            from.SendMessage("Ты копаешь некоторое время, но порода не поддаётся.");
        }

        if (reserve <= 0)
        {
            SurfaceReserve.Remove(key);
            DepletedUntil[key] = Core.Now + RespawnDelay;
            from.EndAction(typeof(MahaonMiningSwings));
            from.SendMessage(0x22, "Здесь больше нет руды.");

            if (allowMineOffer && MineComplexSystem.IsDiggableMountainGraphic(map, targetLoc) &&
                MineComplexSystem.IsWallSegmentStraightEnough(map, targetLoc, facing))
            {
                from.SendGump(new MineSuggestionGump(from, standingLoc, targetLoc, facing, map));
            }
            else if (allowMineOffer)
            {
                from.SendMessage("Стена здесь неровная — тут не заложить нормальный вход в шахту.");
            }
            else if (isInsideMine && from.Backpack?.FindItemByType<MahaonChisel>() != null)
            {
                // This is mine floor, not open ground — offer to dig a ladder down instead
                // of a brand new mine entrance.
                from.SendGump(new LadderSuggestionGump(from, targetLoc, facing, targetLoc, map, onDecline: null));
            }

            return;
        }

        SurfaceReserve[key] = reserve;

        Timer.DelayCall(SwingDelay, () => DoSurfaceSwing(from, targetLoc, standingLoc, facing, map, tool, allowMineOffer, isInsideMine));
    }

    /// <summary>Matched-metal pick doubles the yield — iron pick on iron ore, agapite pick
    /// on agapite ore, etc. Any other tool (or the wrong metal) gets no bonus.
    ///
    /// Real ore drops are MahaonOre now (one class + a MahaonMetal field — see MahaonOre.cs),
    /// not the old per-tier vanilla ore classes MahaonResourceTiers.OreTable/OreToolBonus was
    /// built against, so comparing ore.GetType() against those never matched — this bonus was
    /// dead for every actual drop. Matches on MahaonMetal instead: the tool's reforged metal
    /// (MahaonMetalTracker, set by SmithHammer's "перековка") if it has one, else Iron — every
    /// tool is crafted plain-iron first under the current craft-then-reforge flow, so an
    /// unreforged pick still counts as an iron pick on iron ore, same baseline as before.
    /// OreToolBonus/OreTable stick around for MineComplexSystem's vanilla-typed vein fallback
    /// (CreateVeinResource's OreTiers branch), a separate, much rarer path.</summary>
    private static void ApplyToolBonus(Item tool, Item ore)
    {
        if (tool is not BaseWeapon weapon)
        {
            return;
        }

        double bonus;

        if (ore is MahaonOre mahaonOre)
        {
            var toolMetal = MahaonMetalTracker.GetMetal(weapon) ?? MahaonMetal.Iron;
            bonus = toolMetal == mahaonOre.Metal ? MahaonResourceTiers.ToolMatchBonus : 1.0;
        }
        else
        {
            bonus = MahaonResourceTiers.OreToolBonus(weapon.Resource, ore.GetType());
        }

        if (bonus > 1.0)
        {
            ore.Amount = (int)(ore.Amount * bonus);
        }
    }

    private static bool CanContinue(Mobile from, Point3D anchor) =>
        !from.Deleted && from.Alive && from.InRange(anchor, 3);
}
