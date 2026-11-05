/// <summary>
///     Ported from ServUO (Scripts/Services/ExploringTheDeep/Generate.cs). Places every static piece of the
///     Exploring the Deep content: the sunken ship at Gravewater Lake, the supply lockers and their rings of
///     barrels, the tinker's house and its artefacts, the Ice Dungeon lair, the Sorcerer's Dungeon puzzle, the
///     quest NPCs and the readable scrolls. Run with <c>[GenExploringTheDeep</c>, remove with
///     <c>[DelExploringTheDeep</c>.
///
///     Dropped, because they depend on distribution data that is not part of this port:
///     <list type="bullet">
///         <item>
///             <c>[XmlLoad Spawns/GravewaterLake.xml</c> and its
///             <c>SpawnerPersistence.RemoveSpawnsFromXmlFile("Spawns", "GravewaterLake")</c> counterpart. The
///             XML spawner file lives under <c>Distribution/Data/Spawns</c>; without it this command places
///             everything else.
///         </item>
///         <item>
///             The <c>XmlSpawner</c>-based quest NPC placements: each NPC is created directly at the
///             spawner's location, and the single rock mite spawner is five explicit spawns.
///         </item>
///     </list>
/// </summary>
using System;
using Server.Commands;
using Server.Items;
using Server.Logging;
using Server.Mobiles;

namespace Server.Engines.ExploringTheDeep
{
    public static class GenerateExploringTheDeep
    {
        public static readonly string EntityName = "exploringthedeep";

        public static void Initialize()
        {
            EventSink.ServerStarted += OnServerStarted;
        }

        /// <summary>
        ///     Подземелье и квестовая линия — часть мира: при старте проверяем, стоит ли лебёдка у доку, и если
        ///     нет, разворачиваем всё заново. Проверка идемпотентная, поэтому на обычной загрузке ничего не
        ///     дублируется.
        /// </summary>
        private static void OnServerStarted() => Timer.DelayCall(TimeSpan.FromSeconds(12), EnsureSetup);

        private static void EnsureSetup()
        {
            if (HasWinch())
            {
                return;
            }

            try
            {
                Generate((Mobile)null);
                Logger.Information("Exploring the Deep is set up");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Exploring the Deep setup failed");
            }
        }

        private static readonly ILogger Logger = LogFactory.GetLogger(typeof(GenerateExploringTheDeep));

        private static bool HasWinch()
        {
            foreach (var item in Map.Trammel.GetItemsInRange(new Point3D(6310, 1704, 0), 2))
            {
                if (item is WinchAssembly)
                {
                    return true;
                }
            }

            return false;
        }

        public static void Generate(CommandEventArgs e)
        {
            Mobile m = e.Mobile;

            Delete(e);
            Generate(m);

            m.SendMessage("Exploring the Deep Quest Line Generated!");
        }

        public static void Delete(CommandEventArgs e)
        {
            WeakEntityCollection.Delete(EntityName);
            WeakEntityCollection.Delete(WinchAssembly.EntityName);
            WeakEntityCollection.Delete(SorcerersPlateController.EntityName);
        }

        public static void Generate(Mobile m)
        {
            #region Gravewater Lake Finish

            WinchAssembly.Generate();

            // StorageLocker

            StorageLocker storagelocker = new StorageLocker(Parts.Flywheel);
            WeakEntityCollection.Add(EntityName, storagelocker);
            storagelocker.MoveToWorld(new Point3D(6421, 1753, 0), Map.Trammel);
            storagelocker.Active = true;

            storagelocker = new StorageLocker(Parts.BearingAssembly);
            WeakEntityCollection.Add(EntityName, storagelocker);
            storagelocker.MoveToWorld(new Point3D(6441, 1753, 0), Map.Trammel);
            storagelocker.Active = true;

            storagelocker = new StorageLocker(Parts.PowerCore);
            WeakEntityCollection.Add(EntityName, storagelocker);
            storagelocker.MoveToWorld(new Point3D(6441, 1733, 0), Map.Trammel);
            storagelocker.Active = true;

            storagelocker = new StorageLocker(Parts.WireSpool);
            WeakEntityCollection.Add(EntityName, storagelocker);
            storagelocker.MoveToWorld(new Point3D(6421, 1733, 0), Map.Trammel);
            storagelocker.Active = true;

            Item door = new LightWoodDoor(DoorFacing.SouthCW);
            WeakEntityCollection.Add(EntityName, door);
            door.Hue = 2952;
            door.MoveToWorld(new Point3D(6427, 1735, 0), Map.Trammel);

            door = new LightWoodDoor(DoorFacing.SouthCW);
            WeakEntityCollection.Add(EntityName, door);
            door.Hue = 2952;
            door.MoveToWorld(new Point3D(6427, 1752, 0), Map.Trammel);

            door = new LightWoodDoor(DoorFacing.SouthCCW);
            WeakEntityCollection.Add(EntityName, door);
            door.Hue = 2952;
            door.MoveToWorld(new Point3D(6435, 1735, 0), Map.Trammel);

            door = new LightWoodDoor(DoorFacing.SouthCCW);
            WeakEntityCollection.Add(EntityName, door);
            door.Hue = 2952;
            door.MoveToWorld(new Point3D(6435, 1752, 0), Map.Trammel);

            door = new LightWoodDoor(DoorFacing.WestCW);
            WeakEntityCollection.Add(EntityName, door);
            door.Hue = 2952;
            door.MoveToWorld(new Point3D(6431, 1727, 0), Map.Trammel);

            door = new LightWoodDoor(DoorFacing.EastCCW);
            WeakEntityCollection.Add(EntityName, door);
            door.Hue = 2952;
            door.MoveToWorld(new Point3D(6432, 1727, 0), Map.Trammel);

            Static decor = new Static(0x1EAF);
            WeakEntityCollection.Add(EntityName, decor);
            decor.MoveToWorld(new Point3D(6310, 1704, 11), Map.Trammel);

            decor = new Static(0x1ED5);
            WeakEntityCollection.Add(EntityName, decor);
            decor.MoveToWorld(new Point3D(6310, 1705, -5), Map.Trammel);

            decor = new Static(0x10A4);
            decor.MoveToWorld(new Point3D(6310, 1703, 8), Map.Trammel);
            WeakEntityCollection.Add(EntityName, decor);

            decor = new Static(0x2E3D);
            decor.MoveToWorld(new Point3D(6311, 1703, 19), Map.Trammel);
            WeakEntityCollection.Add(EntityName, decor);

            decor = new Static(0x3A8);
            decor.MoveToWorld(new Point3D(6309, 1704, 20), Map.Trammel);
            WeakEntityCollection.Add(EntityName, decor);

            decor = new Static(0x3A8);
            decor.MoveToWorld(new Point3D(6310, 1704, 20), Map.Trammel);
            WeakEntityCollection.Add(EntityName, decor);

            decor = new Static(0x3A6);
            decor.MoveToWorld(new Point3D(6309, 1703, 24), Map.Trammel);
            WeakEntityCollection.Add(EntityName, decor);

            decor = new Static(0x3A6);
            decor.MoveToWorld(new Point3D(6310, 1703, 24), Map.Trammel);
            WeakEntityCollection.Add(EntityName, decor);

            Item ladder = new ShipLadder(new Point3D(6302, 1672, 0), Map.Trammel, 0x08A6);
            ladder.MoveToWorld(new Point3D(6431, 1699, 0), Map.Trammel);
            WeakEntityCollection.Add(EntityName, ladder);

            ladder = new ShipLadder(new Point3D(6432, 1699, 0), Map.Trammel, 0x08A6);
            ladder.MoveToWorld(new Point3D(6304, 1672, -5), Map.Trammel);
            WeakEntityCollection.Add(EntityName, ladder);

            ladder = new ShipLadder(new Point3D(6292, 1720, 0), Map.Trammel, 0x08A1);
            ladder.MoveToWorld(new Point3D(6400, 1656, 0), Map.Trammel);
            WeakEntityCollection.Add(EntityName, ladder);

            ladder = new ShipLadder(new Point3D(1699, 1646, -115), Map.Malas, 0x14FA);
            ladder.MoveToWorld(new Point3D(6278, 1773, 0), Map.Trammel);
            WeakEntityCollection.Add(EntityName, ladder);

            Item sign = new ShipSign(0xBD2, 1154461); // Use Ladder to Return to Foredeck
            sign.MoveToWorld(new Point3D(6400, 1658, 0), Map.Trammel);
            WeakEntityCollection.Add(EntityName, sign);

            sign = new ShipSign(0xBCF, 1154492); // Use the rope to return to the surface
            sign.MoveToWorld(new Point3D(6278, 1773, 0), Map.Trammel);
            WeakEntityCollection.Add(EntityName, sign);

            sign = new ShipSign(0xBD1, 1154463); // Warning! Only those with proper gear may enter the lake for salvage operations! Enter at your own risk! No Pets!
            sign.MoveToWorld(new Point3D(1698, 1566, -110), Map.Malas);
            WeakEntityCollection.Add(EntityName, sign);

            // CaptainsLogScroll
            Item scroll = new CaptainsLogScroll();
            scroll.MoveToWorld(new Point3D(6430, 1743, 0), Map.Trammel);
            WeakEntityCollection.Add(EntityName, scroll);

            Item tele = new Teleporter(new Point3D(6445, 1743, 0), Map.Trammel);
            tele.MoveToWorld(new Point3D(6321, 1710, -35), Map.Trammel);
            WeakEntityCollection.Add(EntityName, tele);

            tele = new Teleporter(new Point3D(6445, 1743, 0), Map.Trammel);
            tele.MoveToWorld(new Point3D(6321, 1711, -35), Map.Trammel);
            WeakEntityCollection.Add(EntityName, tele);

            tele = new Teleporter(new Point3D(6322, 1710, -35), Map.Trammel);
            tele.MoveToWorld(new Point3D(6447, 1741, 1), Map.Trammel);
            WeakEntityCollection.Add(EntityName, tele);

            tele = new Teleporter(new Point3D(6322, 1710, -35), Map.Trammel);
            tele.MoveToWorld(new Point3D(6447, 1742, 1), Map.Trammel);
            WeakEntityCollection.Add(EntityName, tele);

            tele = new Teleporter(new Point3D(6322, 1710, -35), Map.Trammel);
            tele.MoveToWorld(new Point3D(6447, 1743, 1), Map.Trammel);
            WeakEntityCollection.Add(EntityName, tele);

            tele = new Teleporter(new Point3D(6322, 1710, -35), Map.Trammel);
            tele.MoveToWorld(new Point3D(6447, 1744, 1), Map.Trammel);
            WeakEntityCollection.Add(EntityName, tele);

            tele = new Teleporter(new Point3D(6322, 1710, -35), Map.Trammel);
            tele.MoveToWorld(new Point3D(6447, 1745, 1), Map.Trammel);
            WeakEntityCollection.Add(EntityName, tele);

            tele = new Whirlpool(new Point3D(6274, 1787, 0), Map.Trammel);
            tele.MoveToWorld(new Point3D(1700, 1638, -115), Map.Malas);
            WeakEntityCollection.Add(EntityName, tele);

            Item item = new AnkhWest();
            item.MoveToWorld(new Point3D(1694, 1562, -109), Map.Malas);
            WeakEntityCollection.Add(EntityName, item);

            item = new DungeonHitchingPost();
            item.MoveToWorld(new Point3D(1702, 1552, -109), Map.Malas);
            WeakEntityCollection.Add(EntityName, item);

            #endregion

            #region Quester Spawns

            Mobile quester = new GipsyGemologist();
            quester.MoveToWorld(new Point3D(1509, 618, -16), Map.Ilshenar);
            WeakEntityCollection.Add(EntityName, quester);

            quester = new ChampHuthwait();
            quester.MoveToWorld(new Point3D(2995, 635, 0), Map.Trammel);
            WeakEntityCollection.Add(EntityName, quester);

            quester = new JosefSkimmons();
            quester.MoveToWorld(new Point3D(2630, 2092, 10), Map.Trammel);
            WeakEntityCollection.Add(EntityName, quester);

            quester = new MadelineHarte();
            quester.MoveToWorld(new Point3D(1364, 3780, 0), Map.Trammel);
            WeakEntityCollection.Add(EntityName, quester);

            quester = new CousteauPerron();
            quester.MoveToWorld(new Point3D(5212, 2314, 28), Map.Trammel);
            WeakEntityCollection.Add(EntityName, quester);

            quester = new HeplerPaulson();
            quester.MoveToWorld(new Point3D(2039, 2842, 0), Map.Trammel);
            WeakEntityCollection.Add(EntityName, quester);

            #endregion
             
            #region Custeau Perron House
            door = new CusteauPerronHouseDoor();
            WeakEntityCollection.Add(EntityName, door);
            door.MoveToWorld(new Point3D(1651, 1551, 25), Map.Trammel);

            scroll = new MasterThinkerSchematics();
            scroll.MoveToWorld(new Point3D(1649, 1547, 54), Map.Trammel);
            WeakEntityCollection.Add(EntityName, scroll);

            decor = new Static(0xB7F);
            decor.MoveToWorld(new Point3D(1651, 1549, 45), Map.Trammel);
            WeakEntityCollection.Add(EntityName, decor);

            decor = new Static(0xB80);
            decor.MoveToWorld(new Point3D(1652, 1549, 45), Map.Trammel);
            WeakEntityCollection.Add(EntityName, decor);

            decor = new Static(0xB7E);
            decor.MoveToWorld(new Point3D(1653, 1549, 45), Map.Trammel);
            WeakEntityCollection.Add(EntityName, decor);

            item = new MasterThinkerContoller();
            item.MoveToWorld(new Point3D(1652, 1547, 45), Map.Trammel);
            WeakEntityCollection.Add(EntityName, item);
            #endregion

            #region Ice Dungeon
            Item addon = new CousteauPerronAddon();
            addon.MoveToWorld(new Point3D(5211, 2312, 28), Map.Trammel);
            WeakEntityCollection.Add(EntityName, addon);

            item = new IceCrystals();
            item.MoveToWorld(new Point3D(5799, 234, -5), Map.Trammel);
            WeakEntityCollection.Add(EntityName, item);

            item = new IceCrystals();
            item.MoveToWorld(new Point3D(5799, 235, -4), Map.Trammel);
            WeakEntityCollection.Add(EntityName, item);

            item = new IceCrystals();
            item.MoveToWorld(new Point3D(5800, 236, -8), Map.Trammel);
            WeakEntityCollection.Add(EntityName, item);

            item = new IceCrystals();
            item.MoveToWorld(new Point3D(5802, 234, -4), Map.Trammel);
            WeakEntityCollection.Add(EntityName, item);

            item = new IceCrystals();
            item.MoveToWorld(new Point3D(5801, 239, -7), Map.Trammel);
            WeakEntityCollection.Add(EntityName, item);

            item = new IceCrystals();
            item.MoveToWorld(new Point3D(5801, 240, -4), Map.Trammel);
            WeakEntityCollection.Add(EntityName, item);

            item = new IceCrystals();
            item.MoveToWorld(new Point3D(5803, 243, -2), Map.Trammel);
            WeakEntityCollection.Add(EntityName, item);

            item = new IceCrystals();
            item.MoveToWorld(new Point3D(5806, 244, -6), Map.Trammel);
            WeakEntityCollection.Add(EntityName, item);

            item = new IceCrystals();
            item.MoveToWorld(new Point3D(5807, 240, -2), Map.Trammel);
            WeakEntityCollection.Add(EntityName, item);

            item = new IceCrystals();
            item.MoveToWorld(new Point3D(5808, 237, -3), Map.Trammel);
            WeakEntityCollection.Add(EntityName, item);
            #endregion

            #region Sorcerers Dungeon

            for (var i = 0; i < 5; i++)
            {
                var mite = new RockMite();
                mite.MoveToWorld(
                    new Point3D(122 + Utility.RandomMinMax(-5, 5), 10 + Utility.RandomMinMax(-5, 5), -28), Map.Ilshenar
                );
                WeakEntityCollection.Add(EntityName, mite);
            }

            SorcerersPlateController.Generate();

            #endregion

            #region Scroll
            scroll = new WillemHarteScroll();
            WeakEntityCollection.Add(EntityName, scroll);
            scroll.MoveToWorld(new Point3D(1359, 3779, 7), Map.Trammel);

            scroll = new MadelineHarteScroll();
            WeakEntityCollection.Add(EntityName, scroll);
            scroll.MoveToWorld(new Point3D(1359, 3780, 7), Map.Trammel);

            scroll = new LiamDeFoeScroll();
            WeakEntityCollection.Add(EntityName, scroll);
            scroll.MoveToWorld(new Point3D(1364, 3778, 1), Map.Trammel);

            scroll = new CalculationsScroll();
            scroll.MoveToWorld(new Point3D(2997, 632, 9), Map.Trammel);
            WeakEntityCollection.Add(EntityName, scroll);

            scroll = new SuspicionsScroll();
            scroll.MoveToWorld(new Point3D(2993, 632, 14), Map.Trammel);
            WeakEntityCollection.Add(EntityName, scroll);

            scroll = new SealedLettersScroll();
            scroll.MoveToWorld(new Point3D(2992, 636, 5), Map.Trammel);
            WeakEntityCollection.Add(EntityName, scroll);

            scroll = new LedgerScroll();
            scroll.MoveToWorld(new Point3D(2580, 1118, 5), Map.Trammel);
            WeakEntityCollection.Add(EntityName, scroll);

            scroll = new JournalScroll();
            scroll.MoveToWorld(new Point3D(2578, 1120, 0), Map.Trammel);
            WeakEntityCollection.Add(EntityName, scroll);

            scroll = new JosefSkimmonsScroll();
            scroll.MoveToWorld(new Point3D(2632, 2085, 21), Map.Trammel);
            WeakEntityCollection.Add(EntityName, scroll);

            scroll = new CousteauPerronScroll();
            scroll.MoveToWorld(new Point3D(2632, 2081, 12), Map.Trammel);
            WeakEntityCollection.Add(EntityName, scroll);

            scroll = new EliseTrentScroll();
            scroll.MoveToWorld(new Point3D(2636, 2082, 16), Map.Trammel);
            WeakEntityCollection.Add(EntityName, scroll);

            scroll = new SorcerersScroll();
            scroll.MoveToWorld(new Point3D(101, 42, -22), Map.Ilshenar);
            WeakEntityCollection.Add(EntityName, scroll);
            #endregion
        }
    }
}