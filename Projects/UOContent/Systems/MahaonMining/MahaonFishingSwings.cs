using System;
using System.Collections.Generic;
using Server.Items;
using Server.Systems.MahaonCombat;

namespace Server.Systems.MahaonMining;

/// <summary>
///     Drives real per-cast fishing — animation, a real skill check, a small yield, repeat —
///     same idea as MahaonMiningSwings/MahaonLumberjackingSwings: a fishing spot has a real
///     reserve that depletes and respawns, instead of vanilla's single click → single roll →
///     done. Keeps working automatically until the spot runs dry or something interrupts it.
/// </summary>
public static class MahaonFishingSwings
{
    private static readonly TimeSpan SwingDelay = TimeSpan.FromSeconds(2.0);

    // Same idea as ore veins — a spot holds a real amount before it's fished out, then
    // needs time to recover instead of refreshing the instant it's empty.
    private static readonly Dictionary<(Map map, int x, int y), int> SpotReserve = new();
    private static readonly Dictionary<(Map map, int x, int y), DateTime> DepletedUntil = new();

    private const int SpotReserveMin = 6;
    private const int SpotReserveMax = 16;
    private static readonly TimeSpan RespawnDelay = TimeSpan.FromMinutes(8);

    // Occasionally something other than plain fish comes up — kept simple on purpose,
    // nothing from the rare/SOS/treasure-map side of vanilla fishing here.
    private const double JunkChance = 0.08;

    public static void StartFishing(Mobile from, Point3D targetLoc, Map map, Item tool)
    {
        var key = (map, targetLoc.X, targetLoc.Y);

        if (DepletedUntil.TryGetValue(key, out var until) && Core.Now < until)
        {
            from.SendMessage("Здесь недавно всё выловили — рыба ещё не вернулась.");
            return;
        }

        if (!from.CanBeginAction(typeof(MahaonFishingSwings)))
        {
            return; // already mid-cast-chain
        }

        from.BeginAction(typeof(MahaonFishingSwings));
        DoFishingSwing(from, targetLoc, map, tool);
    }

    private static void DoFishingSwing(Mobile from, Point3D targetLoc, Map map, Item tool)
    {
        if (!CanContinue(from, targetLoc) || tool?.Deleted != false || !tool.IsChildOf(from))
        {
            from.EndAction(typeof(MahaonFishingSwings));
            return;
        }

        from.Direction = from.GetDirectionTo(targetLoc);
        from.Animate(12, 5, 1, true, false, 0); // classic UO fishing cast animation
        Effects.PlaySound(targetLoc, map, 0x364); // the real splash sound — also drives the client's own splash VFX

        var key = (map, targetLoc.X, targetLoc.Y);
        if (!SpotReserve.TryGetValue(key, out var reserve))
        {
            reserve = Utility.RandomMinMax(SpotReserveMin, SpotReserveMax);
        }

        if (from.CheckSkill(SkillName.Fishing, 0.0, 100.0))
        {
            if (Utility.RandomDouble() < JunkChance)
            {
                var junk = PickJunk();

                if (from.Backpack?.TryDropItem(from, junk, false) != true)
                {
                    junk.MoveToWorld(from.Location, map);
                }

                from.SendMessage(0x59, $"Попалось: {junk.GetType().Name}.");
            }
            else
            {
                var fish = new Fish();

                // Was missing entirely — FishingSpecializationSystem.OnFishCaught was only
                // wired into vanilla Fishing.cs's harvest completion, which HarvestTarget.cs
                // never reaches for a real fishing-tool click (redirected here instead,
                // same pattern already fixed for Mining/Lumberjacking), so "Рыбак мелководья"/
                // "Рыбак глубин" could never actually grow from real play.
                var deepWater = SpecialFishingNet.FullValidation(map, targetLoc.X, targetLoc.Y);
                FishingSpecializationSystem.OnFishCaught(from, fish, deepWater);

                if (from.Backpack?.TryDropItem(from, fish, false) != true)
                {
                    fish.MoveToWorld(from.Location, map);
                }

                from.SendMessage(0x59, "Ты вылавливаешь рыбу.");
            }

            reserve--;
        }
        else
        {
            from.SendMessage("Клёва нет — леска пуста.");
        }

        if (reserve <= 0)
        {
            SpotReserve.Remove(key);
            DepletedUntil[key] = Core.Now + RespawnDelay;
            from.EndAction(typeof(MahaonFishingSwings));
            from.SendMessage(0x22, "Рыба здесь закончилась.");
            return;
        }

        SpotReserve[key] = reserve;

        Timer.DelayCall(SwingDelay, () => DoFishingSwing(from, targetLoc, map, tool));
    }

    private static Item PickJunk() => Utility.RandomBool() ? new Boots() : new MessageInABottle();

    private static bool CanContinue(Mobile from, Point3D anchor) =>
        !from.Deleted && from.Alive && from.InRange(anchor, 6);
}
