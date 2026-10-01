using System;
using Server.Engines.Quests.Hag;
using Server.Items;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Engines.Harvest
{
    public class HarvestTarget : Target
    {
        private readonly HarvestSystem m_System;
        private readonly Item m_Tool;

        public HarvestTarget(Item tool, HarvestSystem system) : base(-1, true, TargetFlags.None)
        {
            m_Tool = tool;
            m_System = system;

            DisallowMultis = true;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (m_System is Mining && targeted is Mobile self && self == from)
            {
                var underfoot = ResolveUnderfoot(from);
                if (underfoot != null)
                {
                    targeted = underfoot;
                }
            }

            if (m_System is Mining && targeted is MineRockWall wall)
            {
                if (!from.InRange(wall.GetWorldLocation(), 2))
                {
                    from.SendMessage("Слишком далеко.");
                    return;
                }

                Systems.MahaonMining.MahaonMiningSwings.StartWallSwings(from, wall, m_Tool);
                return;
            }

            if (m_System is Mining && targeted is MineFloorTile floorTile)
            {
                if (!from.InRange(floorTile.GetWorldLocation(), 2))
                {
                    from.SendMessage("Слишком далеко.");
                    return;
                }

                // Ordinary ore source, same as any other tile — just doesn't offer to spawn
                // a new mine on top of an existing one when it runs dry.
                Systems.MahaonMining.MahaonMiningSwings.StartSurfaceSwings(from, floorTile.GetWorldLocation(), from.Map, m_Tool, allowMineOffer: false, isInsideMine: true);
                return;
            }

            // Any land tile mines through our real swing system now, not just the specific
            // "self-dig" mountain graphics — those specific graphics are only special in
            // that running them dry offers to start a real mine (checked inside
            // StartSurfaceSwings itself).
            if (m_System is Mining && targeted is LandTarget landTarget)
            {
                var loc = new Point3D(landTarget.X, landTarget.Y, landTarget.Z);

                if (!from.InRange(loc, 2))
                {
                    from.SendMessage("Слишком далеко.");
                    return;
                }

                var tileFlags = TileData.LandTable[landTarget.TileID & TileData.MaxLandValue].Flags;
                if ((tileFlags & TileFlag.Impassable) == 0)
                {
                    from.SendMessage("Здесь нечего копать — это не гора.");
                    return;
                }

                Systems.MahaonMining.MahaonMiningSwings.StartSurfaceSwings(from, loc, from.Map, m_Tool);
                return;
            }

            // Any static overlay mines through our swing system too — not just the
            // whitelisted mountain graphics. The whitelist only controls whether running a
            // spot dry offers to start a real mine; everything else still mines normally
            // instead of falling through to the old vanilla one-shot pile.
            // Могилы и надгробия сюда не попадают — у них своя ветка ниже (копка кладбищ).
            // Список берётся оттуда же целиком: раньше здесь были перечислены руками шесть
            // земляных холмиков, а полсотни надгробий уходили в обычную добычу руды и
            // выдавали из могилы железо.
            if (m_System is Mining && targeted is StaticTarget genericStatic &&
                Array.IndexOf(Systems.MahaonGraves.GraveDigging.GraveTiles, genericStatic.ItemID) < 0)
            {
                var loc = new Point3D(genericStatic.X, genericStatic.Y, genericStatic.Z);

                if (!from.InRange(loc, 2))
                {
                    from.SendMessage("Слишком далеко.");
                    return;
                }

                var isWhitelisted = Array.IndexOf(Systems.MahaonMining.MineComplexSystem.DiggableMountainGraphics, genericStatic.ItemID) >= 0;
                Systems.MahaonMining.MahaonMiningSwings.StartSurfaceSwings(from, loc, from.Map, m_Tool, allowMineOffer: isWhitelisted);
                return;
            }

            if (m_System is Mining && targeted is StaticTarget target)
            {
                var itemID = target.ItemID;

                // grave
                if (Array.IndexOf(Systems.MahaonGraves.GraveDigging.GraveTiles, itemID) >= 0)
                {
                    // Задание Ведьмы важнее обычной копки: у него своя, разовая награда, и
                    // перебивать её костями было бы обидно.
                    if (from is PlayerMobile player)
                    {
                        var qs = player.Quest;

                        if (qs is WitchApprenticeQuest)
                        {
                            var obj = qs.FindObjective<FindIngredientObjective>();

                            if (obj?.Completed == false && obj.Ingredient == Ingredient.Bones)
                            {
                                // You finish your grim work, finding some of the specific bones listed in the Hag's recipe.
                                player.SendLocalizedMessage(1055037);
                                obj.Complete();

                                return;
                            }
                        }
                    }

                    // Mahaon: раньше на этом всё и заканчивалось — могилы не делали ничего
                    // ни для кого, кроме одного квеста, и лопата по ним просто молчала.
                    // Теперь это полноценная добыча со своим истощением и восстановлением.
                    Systems.MahaonGraves.GraveDigging.System.StartHarvesting(from, m_Tool, targeted);
                    return;
                }
            }

            if (m_System is Lumberjacking && targeted is MahaonTree mahaonTree)
            {
                mahaonTree.OnDoubleClick(from);
                return;
            }

            if (m_System is Lumberjacking && targeted is MahaonTreeFoliage mahaonFoliage)
            {
                mahaonFoliage.ChopWithAxe(from);
                return;
            }

            if (m_System is Lumberjacking && targeted is StaticTarget treeStatic)
            {
                var loc = new Point3D(treeStatic.X, treeStatic.Y, treeStatic.Z);

                if (!from.InRange(loc, 2))
                {
                    from.SendMessage("Слишком далеко.");
                    return;
                }

                Systems.MahaonMining.MahaonLumberjackingSwings.StartTreeSwings(from, loc, treeStatic.ItemID, from.Map, m_Tool);
                return;
            }

            if (m_System is Fishing && targeted is LandTarget fishLand)
            {
                var loc = new Point3D(fishLand.X, fishLand.Y, fishLand.Z);

                if (!from.InRange(loc, 6))
                {
                    from.SendMessage("Слишком далеко.");
                    return;
                }

                var tileFlags = TileData.LandTable[fishLand.TileID & TileData.MaxLandValue].Flags;
                if ((tileFlags & TileFlag.Wet) == 0)
                {
                    from.SendMessage("Здесь негде рыбачить — это не вода.");
                    return;
                }

                Systems.MahaonMining.MahaonFishingSwings.StartFishing(from, loc, from.Map, m_Tool);
                return;
            }

            // Shoreline/river-edge water is often a static overlay rather than the land
            // tile itself (see the client-side water-overlay work — same distinction
            // mattered there too), so fishing needs to accept a StaticTarget click the
            // same way it accepts a LandTarget one, not just open-water land tiles.
            if (m_System is Fishing && targeted is StaticTarget fishStatic)
            {
                var loc = new Point3D(fishStatic.X, fishStatic.Y, fishStatic.Z);

                if (!from.InRange(loc, 6))
                {
                    from.SendMessage("Слишком далеко.");
                    return;
                }

                var itemFlags = TileData.ItemTable[fishStatic.ItemID & TileData.MaxItemValue].Flags;
                if ((itemFlags & TileFlag.Wet) == 0)
                {
                    from.SendMessage("Здесь негде рыбачить — это не вода.");
                    return;
                }

                Systems.MahaonMining.MahaonFishingSwings.StartFishing(from, loc, from.Map, m_Tool);
                return;
            }

            if (m_System is Lumberjacking && targeted is IChoppable chopable)
            {
                chopable.OnChop(from);
            }
            else if (m_System is Lumberjacking && targeted is IAxe obj && m_Tool is BaseAxe axe)
            {
                var item = (Item)obj;

                if (!item.IsChildOf(from.Backpack))
                {
                    from.SendLocalizedMessage(1062334); // This item must be in your backpack to be used.
                }
                else if (obj.Axe(from, axe))
                {
                    from.PlaySound(0x13E);
                }
            }
            else if (m_System is Lumberjacking && targeted is ICarvable carvable)
            {
                carvable.Carve(from, m_Tool);
            }
            else if (m_System is Lumberjacking && FurnitureAttribute.Check(targeted as Item))
            {
                DestroyFurniture(from, (Item)targeted);
            }
            else if (m_System is Mining && targeted is TreasureMap map)
            {
                map.OnBeginDig(from);
            }
            else
            {
                m_System.StartHarvesting(from, m_Tool, targeted);
            }
        }

        private void DestroyFurniture(Mobile from, Item item)
        {
            if (!from.InRange(item.GetWorldLocation(), 3))
            {
                from.SendLocalizedMessage(500446); // That is too far away.
                return;
            }

            if (!item.IsChildOf(from.Backpack) && !item.Movable)
            {
                from.SendLocalizedMessage(500462); // You can't destroy that while it is here.
                return;
            }

            from.SendLocalizedMessage(500461); // You destroy the item.
            Effects.PlaySound(item.GetWorldLocation(), item.Map, 0x3B3);

            if (item is Container container)
            {
                if (container is TrappableContainer trappableContainer)
                {
                    trappableContainer.ExecuteTrap(from);
                }

                container.Destroy();
            }
            else
            {
                item.Delete();
            }
        }

        private static object ResolveUnderfoot(Mobile from)
        {
            var map = from.Map;
            if (map == null)
            {
                return null;
            }

            var loc = from.Location;

            foreach (var item in map.GetItemsInRange<Item>(loc, 0))
            {
                if (item.Location == loc && item is MineRockWall or MineFloorTile)
                {
                    return item;
                }
            }

            return new LandTarget(loc, map);
        }
    }
}
