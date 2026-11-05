using Server.Engines.MLQuests.Objectives;
using Server.Engines.MLQuests.Rewards;
using Server.Engines.Quests.Eodon;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.MLQuests.Definitions
{
    /// <summary>
    ///     The "Myrmidex Threat" Eodon quests, ported from ServUO (Scripts/Quests/Eodon/MyrmidexThreat/Quests.cs).
    ///
    ///     Dropped: <c>MyrmidexInvasionSystem.CanRecieveQuest(Owner, Allegiance)</c>. This shard has no Myrmidex
    ///     Invasion allegiance system, so the allegiance gate (and its localized refusal messages) is gone and every
    ///     quest is offered directly.
    ///     Substituted: <c>GettingEvenQuest</c>. Its objectives counted wins of the Fire Island casino games
    ///     (<c>ChucklesLuck</c>/<c>HiMiddleLow</c>/<c>DiceRider</c> from <c>Server.Engines.ResortAndCasino</c>), none of
    ///     which exist here; the gambler now asks for three bottles of wine instead, which keeps the quest item reward
    ///     (the insecticide) reachable.
    ///     Dropped: the treasure-map/shipwreck "on complete" hooks (<c>TreasureMap.OnMapComplete</c>,
    ///     <c>SOS.OnSOSComplete</c>) do not exist here, so the stasis chamber activator and regulator are handed over
    ///     together with the map and the message in a bottle that carry the quest item.
    /// </summary>
    public class TheZealotryOfZipactriotlQuest : MLQuest
    {
        public TheZealotryOfZipactriotlQuest()
        {
            Activated = true;
            Title = 1156751;
            Description = 1156752;
            RefusalMessage = 1156753;
            InProgressMessage = 1156754;
            CompletionMessage = 1156755;

            Objectives.Add(new CollectObjective(1, typeof(StasisChamberPowerCore), "силовой блок стазис-камеры"));
            Objectives.Add(new CollectObjective(1, typeof(StasisChamberActivator), "активатор стазис-камеры"));
            Objectives.Add(new CollectObjective(1, typeof(StasisChamberRegulator), "регулятор стазис-камеры"));
            Objectives.Add(new CollectObjective(1, typeof(StasisChamberStator), "статор стазис-камеры"));

            Rewards.Add(new DummyReward(1156756)); // A step closer to destroying Zipactriotl...
        }

        public override System.Type NextQuest => typeof(DestructionOfZipactriotlQuest);
    }

    public class DestructionOfZipactriotlQuest : MLQuest
    {
        public DestructionOfZipactriotlQuest()
        {
            Activated = true;
            Title = 1156757;
            Description = 1156758;
            RefusalMessage = 1156760;
            InProgressMessage = 1156761;
            CompletionMessage = 1156762;

            Objectives.Add(new KillObjective(1, new[] { typeof(Zipactriotl) }, "Зипактриотль"));

            Rewards.Add(new ItemReward("награда Мирмидекс", typeof(MyrmidexRewardBag), 1));
        }

        public override bool IsChainTriggered => true;
    }

    public class HiddenTreasuresQuest : MLQuest
    {
        public HiddenTreasuresQuest()
        {
            Activated = true;
            Title = 1156800;
            Description = 1156801;
            RefusalMessage = 1156804;
            InProgressMessage = 1156803;
            CompletionMessage = 1156805;

            Objectives.Add(new CollectObjective(1, typeof(PerfectEmerald), "безупречный изумруд"));
            Objectives.Add(new CollectObjective(1, typeof(WhitePearl), "белый жемчуг"));
            Objectives.Add(new CollectObjective(1, typeof(BrilliantAmber), "блистательный янтарь"));

            Rewards.Add(new ItemReward("особая карта сокровищ", typeof(HiddenTreasuresTreasureMap), 1));
        }

        public override void GetRewards(MLQuestInstance instance)
        {
            instance.Player.AddToBackpack(new StasisChamberActivator());
            base.GetRewards(instance);
        }
    }

    public class TheSaltySeaQuest : MLQuest
    {
        public TheSaltySeaQuest()
        {
            Activated = true;
            Title = 1156807;
            Description = 1156808;
            RefusalMessage = 1156812;
            InProgressMessage = 1156813;
            CompletionMessage = 1156814;

            Objectives.Add(new KillObjective(10, new[] { typeof(WaterElemental) }, "водяной элементал"));
            Objectives.Add(new KillObjective(10, new[] { typeof(SeaSerpent) }, "морской змей"));
            Objectives.Add(new KillObjective(10, new[] { typeof(DeepSeaSerpent) }, "глубинный морской змей"));
            Objectives.Add(new KillObjective(5, new[] { typeof(Kraken) }, "кракен"));

            Rewards.Add(new ItemReward("особое послание в бутылке", typeof(SaltySeaMIB), 1));
        }

        public override void GetRewards(MLQuestInstance instance)
        {
            instance.Player.AddToBackpack(new StasisChamberRegulator());
            base.GetRewards(instance);
        }
    }

    public class ATinkersTaskQuest : MLQuest
    {
        public ATinkersTaskQuest()
        {
            Activated = true;
            Title = 1156815;
            Description = 1156816;
            RefusalMessage = 1156817;
            InProgressMessage = 1156818;
            CompletionMessage = 1156819;

            Objectives.Add(new CollectObjective(500, typeof(IronIngot), "железный слиток"));
            Objectives.Add(new CollectObjective(25, typeof(Gears), "шестерёнки"));
            Objectives.Add(new CollectObjective(5, typeof(PowerCrystal), "кристалл силы"));
            Objectives.Add(new CollectObjective(2, typeof(ClockworkAssembly), "часовой механизм"));

            Rewards.Add(new ItemReward(1156628, typeof(StasisChamberStator), 1));
        }
    }

    public class ExterminatingTheInfestationQuest : MLQuest
    {
        public ExterminatingTheInfestationQuest()
        {
            Activated = true;
            Title = 1156763;
            Description = 1156764;
            RefusalMessage = 1156767;
            InProgressMessage = 1156768;
            CompletionMessage = 1156769;

            Objectives.Add(new CollectObjective(1, typeof(BottledMyrmidexPheromone), "флакон феромона мирмидекс"));
            Objectives.Add(new CollectObjective(1, typeof(BottleOfConcentratedInsecticide), "бутыль концентрированного инсектицида"));
            Objectives.Add(new CollectObjective(1, typeof(MyrmidexPopulationReport), "отчёт о численности мирмидекс"));

            Rewards.Add(new DummyReward(1156766)); // A step closer to destroying the Myrmidex Queen...
        }

        public override System.Type NextQuest => typeof(InsecticideAndRegicideQuest);
    }

    public class InsecticideAndRegicideQuest : MLQuest
    {
        public InsecticideAndRegicideQuest()
        {
            Activated = true;
            Title = 1156770;
            Description = 1156771;
            RefusalMessage = 1156767;
            InProgressMessage = 1156773;
            CompletionMessage = 1156774;

            Objectives.Add(new KillObjective(1, new[] { typeof(MyrmidexQueen) }, "королева мирмидекс"));

            Rewards.Add(new ItemReward("награда эодонцев", typeof(EodonianRewardBag), 1));
        }

        public override bool IsChainTriggered => true;
    }

    public class PestControlQuest : MLQuest
    {
        public PestControlQuest()
        {
            Activated = true;
            Title = 1156785;
            Description = 1156776;
            RefusalMessage = 1156787;
            InProgressMessage = 1156788;
            CompletionMessage = 1156789;

            Objectives.Add(new KillObjective(20, new[] { typeof(MyrmidexLarvae) }, "личинка мирмидекс"));
            Objectives.Add(new KillObjective(15, new[] { typeof(MyrmidexDrone) }, "трутень мирмидекс"));
            Objectives.Add(new KillObjective(10, new[] { typeof(MyrmidexWarrior) }, "воин мирмидекс"));

            Rewards.Add(new ItemReward(1156775, typeof(MyrmidexPopulationReport), 1));
        }
    }

    public class GettingEvenQuest : MLQuest
    {
        public GettingEvenQuest()
        {
            Activated = true;
            Title = 1156790;
            Description = 1156791;
            RefusalMessage = 1156796;
            InProgressMessage = 1156797;
            CompletionMessage = 1156798;

            Objectives.Add(new CollectObjective(3, typeof(BeverageBottle), "бутылка вина"));

            Rewards.Add(new ItemReward(1156617, typeof(BottleOfConcentratedInsecticide), 1));
        }
    }

    public class OfVorpalsAndLettacesTheGardnerQuest : MLQuest
    {
        public OfVorpalsAndLettacesTheGardnerQuest()
        {
            Activated = true;
            Title = 1156780;
            Description = 1156781;
            RefusalMessage = 1156782;
            InProgressMessage = 1156783;
            CompletionMessage = 1156784;

            Objectives.Add(new KillObjective(1, new[] { typeof(VorpalBunny) }, "кролик-ворпал"));

            Rewards.Add(new ItemReward(1156620, typeof(BottledMyrmidexPheromone), 1));
        }
    }
}
