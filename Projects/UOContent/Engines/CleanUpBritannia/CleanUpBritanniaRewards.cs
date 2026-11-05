using System.Collections.Generic;
using Server.Engines.VvV;
using Server.Items;

namespace Server.Engines.CleanUpBritannia;

/// <summary>
///     Награды Clean Up Britannia. Таблица перенесена из ServUO целиком; выброшены только те
///     позиции, чьих типов в этом репозитории нет (см. комментарии ниже).
/// </summary>
public static class CleanUpBritanniaRewards
{
    public static List<CollectionItem> Rewards { get; private set; }

    public static void Configure()
    {
        Rewards = new List<CollectionItem>
        {
            new(typeof(Mailbox), 0x4142, 1113927, 0, 1000),
            new(typeof(HumansAndElvesRobe), 0x1F03, 1151202, 0, 1000),
            new(typeof(GargoylesAreOurFriendsRobe), 0x1F03, 1151203, 0, 1000),
            new(typeof(WeArePiratesRobe), 0x1F03, 1151204, 0, 1000),
            new(typeof(FollowerOfBaneRobe), 0x1F03, 1151205, 0, 1000),
            new(typeof(QueenDawnForeverRobe), 0x1F03, 1151206, 0, 1000),

            new(typeof(LillyPad), 0xDBC, 1023516, 0, 5000),
            new(typeof(LillyPads), 0xDBE, 1023518, 0, 5000),
            new(typeof(Mushrooms1), 0x0D0F, 1023340, 0, 5000),
            new(typeof(Mushrooms2), 0x0D12, 1023340, 0, 5000),
            new(typeof(Mushrooms3), 0x0D10, 1023340, 0, 5000),
            new(typeof(Mushrooms4), 0x0D13, 1023340, 0, 5000),
            new(typeof(NocturneEarrings), 0x1F07, 1151243, 0x3E5, 5000),

            new(typeof(SherryTheMouseStatue), 0x20D0, 1080171, 0, 10000),
            new(typeof(ChaosTileDeed), 0x14EF, 1080490, 0, 10000),
            new(typeof(HonestyVirtueTileDeed), 0x14EF, 1080488, 0, 10000),
            new(typeof(CompassionVirtueTileDeed), 0x14EF, 1080481, 0, 10000),
            new(typeof(ValorVirtueTileDeed), 0x14EF, 1080486, 0, 10000),
            new(typeof(SpiritualityVirtueTileDeed), 0x14EF, 1080484, 0, 10000),
            new(typeof(HonorVirtueTileDeed), 0x14EF, 1080485, 0, 10000),
            new(typeof(HumilityVirtueTileDeed), 0x14EF, 1080483, 0, 10000),
            new(typeof(SacrificeVirtueTileDeed), 0x14EF, 1080482, 0, 10000),
            new(typeof(JusticeVirtueTileDeed), 0x14EF, 1080487, 0, 10000),

            new(typeof(KnightsBascinet), 0x140C, 1151247, 1150, 10000),
            new(typeof(KnightsCloseHelm), 0x1408, 1151244, 1150, 10000),
            new(typeof(KnightsFemalePlateChest), 0x1C04, 1151253, 1150, 10000),
            new(typeof(KnightsNorseHelm), 0x140E, 1151245, 1150, 10000),
            new(typeof(KnightsPlateArms), 0x1410, 1151250, 1150, 10000),
            new(typeof(KnightsPlateChest), 0x1415, 1151252, 1150, 10000),
            new(typeof(KnightsPlateGloves), 0x1414, 1151249, 1150, 10000),
            new(typeof(KnightsPlateGorget), 0x1413, 1151248, 1150, 10000),
            new(typeof(KnightsPlateHelm), 0x1412, 1151246, 1150, 10000),
            new(typeof(KnightsPlateLegs), 0x1411, 1151251, 1150, 10000),

            new(typeof(ScoutArms), 0x13DC, 1151257, 1148, 10000),
            new(typeof(ScoutBustier), 0x1C0C, 1151262, 1148, 10000),
            new(typeof(ScoutChest), 0x13DB, 1151258, 1148, 10000),
            new(typeof(ScoutCirclet), 0x2B6E, 1151254, 1148, 10000),
            new(typeof(ScoutFemaleChest), 0x1C02, 1151261, 1148, 10000),
            new(typeof(ScoutGloves), 0x13D5, 1151259, 1148, 10000),
            new(typeof(ScoutGorget), 0x13D6, 1151256, 1148, 10000),
            new(typeof(ScoutLegs), 0x13DA, 1151260, 1148, 10000),
            new(typeof(ScoutSmallPlateJingasa), 0x2784, 1151255, 1148, 10000),

            new(typeof(SorcererArms), 0x13CD, 1151265, 1165, 10000),
            new(typeof(SorcererChest), 0x13CC, 1151266, 1165, 10000),
            new(typeof(SorcererFemaleChest), 0x1C06, 1151267, 1165, 10000),
            new(typeof(SorcererGloves), 0x13C6, 1151268, 1165, 10000),
            new(typeof(SorcererGorget), 0x13C7, 1151264, 1165, 10000),
            new(typeof(SorcererHat), 0x1718, 1151263, 1165, 10000),
            new(typeof(SorcererLegs), 0x13CB, 1151270, 1165, 10000),
            new(typeof(SorcererSkirt), 0x1C08, 1151269, 1165, 10000),

            new(typeof(YuccaTree), 0x0D37, 1023383, 0, 15000),
            new(typeof(TableLamp), 0x49C1, 1151220, 0, 15000),
            new(typeof(Bamboo), 0x246D, 1029324, 0, 15000),

            new(typeof(HorseBardingDeed), 0x14EF, 1080212, 0, 20000),
            new(typeof(ScrollofAlacrity), 0x14EF, 1078604, 1195, 20000),

            new(typeof(SnakeSkinBoots), 0x170B, 1151224, 0x7D9, 20000),
            new(typeof(BootsOfTheLavaLizard), 0x170B, 1151223, 0x674, 20000),
            new(typeof(BootsOfTheIceWyrm), 0x170B, 1151225, 0x482, 20000),
            new(typeof(BootsOfTheCrystalHydra), 0x170B, 1151226, 0x47E, 20000),
            new(typeof(BootsOfTheThrasher), 0x170B, 1151227, 0x497, 20000),

            new(typeof(NaturesTears), 0x0E9C, 1154374, 2075, 20000),
            new(typeof(PrimordialDecay), 0x0E9C, 1154737, 1927, 20000),
            new(typeof(ArachnidDoom), 0x0E9C, 1154738, 1944, 20000),

            new(typeof(SophisticatedElvenTapestry), 0x2D70, 1151222, 0, 50000),
            new(typeof(OrnateElvenTapestry), 0x2D72, 1031633, 0, 50000),
            new(typeof(ChestOfDrawers), 0x0A2C, 1022604, 0, 50000),
            new(typeof(FootedChestOfDrawers), 0x0A30, 1151221, 0, 50000),

            new(typeof(DragonHeadAddonDeed), 0x2234, 1028756, 0, 50000),
            new(typeof(NestWithEggs), 0x1AD4, 1026868, 2415, 50000),

            new(typeof(BestialArms), 0x0302, 1151549, 2010, 50000),
            new(typeof(BestialEarrings), 0x4213, 1151547, 2010, 50000),
            new(typeof(BestialGloves), 0x13C6, 1151230, 2010, 50000),
            new(typeof(BestialGorget), 0x13D6, 1151232, 2010, 50000),
            new(typeof(BestialHelm), 0x1545, 1151229, 2010, 50000),
            new(typeof(BestialKilt), 0x0408, 1151550, 2010, 50000),
            new(typeof(BestialLegs), 0x13CB, 1151231, 2010, 50000),
            new(typeof(BestialNecklace), 0x4210, 1151548, 2010, 50000),

            new(typeof(VirtuososArmbands), 0x0308, 1151563, 1374, 50000),
            new(typeof(VirtuososCap), 0x171C, 1151324, 1374, 50000),
            new(typeof(VirtuososCollar), 0x13C7, 1151323, 1374, 50000),
            new(typeof(VirtuososEarpieces), 0x4213, 1151562, 1374, 50000),
            new(typeof(VirtuososKidGloves), 0x13C6, 1151560, 1374, 50000),
            new(typeof(VirtuososKilt), 0x0408, 1151564, 1374, 50000),
            new(typeof(VirtuososNecklace), 0x4213, 1151561, 1374, 50000),
            new(typeof(VirtuososTunic), 0x13CC, 1151325, 1374, 50000),

            new(typeof(FirePitDeed), 0x29FD, 1080206, 0, 75000),
            new(typeof(PresentationStone), 0x32F2, 1154745, 0, 75000),
            new(typeof(Beehive), 0x091A, 1080263, 0, 80000),
            new(typeof(ArcheryButteDeed), 0x100B, 1024106, 0, 80000),

            new(typeof(NovoBleue), 0x1086, 1151242, 1165, 150000),
            new(typeof(EtoileBleue), 0x108A, 1151241, 1165, 150000),
            new(typeof(SoleilRouge), 0x1086, 1154382, 1166, 150000),
            new(typeof(LuneRouge), 0x108A, 1154380, 1166, 150000)
        };

        if (Core.SA)
        {
            Rewards.AddRange(
                new[]
                {
                    new CollectionItem(typeof(IntenseTealPigment), 0xEFF, 1154732, 2691, 250000),
                    new CollectionItem(typeof(TyrianPurplePigment), 0xEFF, 1154735, 2716, 250000),
                    new CollectionItem(typeof(MottledSunsetBluePigment), 0xEFF, 1154734, 2714, 250000),
                    new CollectionItem(typeof(MossyGreenPigment), 0xEFF, 1154731, 2684, 250000),
                    new CollectionItem(typeof(VibrantOcherPigment), 0xEFF, 1154736, 2725, 250000),
                    new CollectionItem(typeof(OliveGreenPigment), 0xEFF, 1154733, 2709, 250000),
                    new CollectionItem(typeof(PolishedBronzePigment), 0xEFF, 1151909, 1944, 250000),
                    new CollectionItem(typeof(GlossyBluePigment), 0xEFF, 1151910, 1916, 250000),
                    new CollectionItem(typeof(BlackAndGreenPigment), 0xEFF, 1151911, 1979, 250000),
                    new CollectionItem(typeof(DeepVioletPigment), 0xEFF, 1151912, 1929, 250000),
                    new CollectionItem(typeof(AuraOfAmberPigment), 0xEFF, 1152308, 1967, 250000),
                    new CollectionItem(typeof(MurkySeagreenPigment), 0xEFF, 1152309, 1992, 250000),
                    new CollectionItem(typeof(ShadowyBluePigment), 0xEFF, 1152310, 1960, 250000),
                    new CollectionItem(typeof(GleamingFuchsiaPigment), 0xEFF, 1152311, 1930, 250000),
                    new CollectionItem(typeof(GlossyFuchsiaPigment), 0xEFF, 1152347, 1919, 250000),
                    new CollectionItem(typeof(DeepBluePigment), 0xEFF, 1152348, 1939, 250000),
                    new CollectionItem(typeof(VibranSeagreenPigment), 0xEFF, 1152349, 1970, 250000),
                    new CollectionItem(typeof(MurkyAmberPigment), 0xEFF, 1152350, 1989, 250000),
                    new CollectionItem(typeof(VibrantCrimsonPigment), 0xEFF, 1153386, 1964, 250000),
                    new CollectionItem(typeof(ReflectiveShadowPigment), 0xEFF, 1153387, 1910, 250000),
                    new CollectionItem(typeof(StarBluePigment), 0xEFF, 1154121, 2723, 250000),
                    new CollectionItem(typeof(MotherOfPearlPigment), 0xEFF, 1154120, 2720, 250000),
                    new CollectionItem(typeof(LiquidSunshinePigment), 0xEFF, 1154213, 1923, 250000),
                    new CollectionItem(typeof(DarkVoidPigment), 0xEFF, 1154214, 2068, 250000),

                    new CollectionItem(typeof(LuckyCharm), 0x2F5B, 1154739, 1923, 300000),
                    new CollectionItem(typeof(SoldiersMedal), 0x2F5B, 1154740, 1902, 300000),
                    new CollectionItem(typeof(DuelistsEdge), 0x2F58, 1154741, 1902, 300000),
                    new CollectionItem(typeof(NecromancersPhylactery), 0x2F5A, 1154742, 1912, 300000),
                    new CollectionItem(typeof(WizardsCurio), 0x2F58, 1154743, 1912, 300000),
                    new CollectionItem(typeof(MysticsMemento), 0x2F5B, 1154744, 1912, 300000)
                }
            );
        }
    }
}
