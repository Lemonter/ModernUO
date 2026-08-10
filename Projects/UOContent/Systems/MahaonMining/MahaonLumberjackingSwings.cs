using System;
using System.Collections.Generic;
using Server.Items;

namespace Server.Systems.MahaonMining;

/// <summary>
///     Drives real per-swing chopping on a tree — same idea as MahaonMiningSwings, just for
///     Lumberjacking: animation, a real skill check, a small yield, repeat automatically
///     until the tree's reserve runs out or something interrupts it. Replaces the vanilla
///     "one click, one pile of logs" behavior.
///
///     Real world trees are map-baked static tiles (StaticTarget when clicked) — they don't
///     have a Serial the way a real Item does, so everything here tracks by location
///     instead, same approach as MahaonMiningSwings uses for open ground.
/// </summary>
public static class MahaonLumberjackingSwings
{
    private static readonly TimeSpan SwingDelay = TimeSpan.FromSeconds(1.6);

    private static readonly Dictionary<(Map map, int x, int y, int z), int> TreeReserve = new();
    private static readonly Dictionary<(Map map, int x, int y, int z), DateTime> DepletedUntil = new();

    private const int ReserveMin = 20;
    private const int ReserveMax = 45;
    private static readonly TimeSpan RespawnDelay = TimeSpan.FromMinutes(10);

    public static void StartTreeSwings(Mobile from, Point3D loc, int treeGraphic, Map map, Item tool)
    {
        var key = (map, loc.X, loc.Y, loc.Z);

        if (DepletedUntil.TryGetValue(key, out var until) && Core.Now < until)
        {
            from.SendMessage("Это дерево недавно срубили под корень — оно ещё не оправилось.");
            return;
        }

        if (!from.CanBeginAction(typeof(MahaonLumberjackingSwings)))
        {
            return;
        }

        from.BeginAction(typeof(MahaonLumberjackingSwings));
        DoTreeSwing(from, loc, treeGraphic, map, tool);
    }

    private static void DoTreeSwing(Mobile from, Point3D loc, int treeGraphic, Map map, Item tool)
    {
        var key = (map, loc.X, loc.Y, loc.Z);

        if (!CanContinue(from, loc))
        {
            from.EndAction(typeof(MahaonLumberjackingSwings));
            from.SendMessage("Вы прекратили рубить.");
            return;
        }

        from.Direction = from.GetDirectionTo(loc);
        from.Animate(13, 5, 1, true, false, 0);
        from.PlaySound(0x13E);

        if (!TreeReserve.TryGetValue(key, out var reserve))
        {
            reserve = Utility.RandomMinMax(ReserveMin, ReserveMax);
        }

        var lumberjackingSkill = from.Skills[SkillName.Lumberjacking].Value;

        if (from.CheckSkill(SkillName.Lumberjacking, 0.0, 100.0))
        {
            var woodType = MahaonResourceTiers.PickWood(lumberjackingSkill);
            var log = (Item)Activator.CreateInstance(woodType, 1);

            if (tool is BaseWeapon axe)
            {
                var bonus = MahaonResourceTiers.WoodToolBonus(axe.Resource, woodType);
                if (bonus > 1.0)
                {
                    log.Amount = (int)(log.Amount * bonus);
                }
            }

            if (from.Backpack?.TryDropItem(from, log, false) != true)
            {
                log.MoveToWorld(from.Location, map);
            }

            from.SendMessage(0x59, $"Ты срубаешь: {log.Amount} x {log.GetType().Name}.");

            reserve--;
        }
        else
        {
            from.SendMessage("Ты рубишь некоторое время, но древесина не поддаётся.");
        }

        if (reserve <= 0)
        {
            TreeReserve.Remove(key);
            DepletedUntil[key] = Core.Now + RespawnDelay;
            from.EndAction(typeof(MahaonLumberjackingSwings));
            from.SendMessage(0x22, "Рубить больше нечего — дерево срублено под корень.");

            Systems.MahaonWorld.StaticOverrideManager.AddOverride(
                loc, map, (ushort)treeGraphic, 0x0E59, RespawnDelay
            );

            return;
        }

        TreeReserve[key] = reserve;

        Timer.DelayCall(SwingDelay, () => DoTreeSwing(from, loc, treeGraphic, map, tool));
    }

    private static bool CanContinue(Mobile from, Point3D loc) =>
        !from.Deleted && from.Alive && from.InRange(loc, 3);
}
