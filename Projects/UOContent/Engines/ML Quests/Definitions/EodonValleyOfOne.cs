using System;
using Server.Engines.MLQuests.Objectives;
using Server.Engines.MLQuests.Rewards;
using Server.Engines.Quests.Eodon;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.MLQuests.Definitions
{
    /// <summary>
    ///     The "Valley of One" Eodon quest line, ported from ServUO (Scripts/Quests/Eodon/Valley of One Quest/Quests.cs).
    ///     The chain starts with Hawkwind (TimeIsOfTheEssenceQuest) and continues with Sir Geoffery.
    ///
    ///     Three ServUO objectives were custom <c>QuestObjective</c> types driven by puzzle items and had no MLQuest
    ///     counterpart, so they are substituted (see the report for the full list):
    ///     <list type="bullet">
    ///         <item><c>EmptyNestQuest</c>: the hatchling-rescue counter became a kill objective on the poachers.</item>
    ///         <item><c>TheGreatVolcanoQuest</c>: the lava-rock mining counter became a kill objective on volcano elementals.</item>
    ///         <item><c>PrideOfTheAmbushQuest</c>: the freed-cub counter became a kill objective on the trappers.</item>
    ///     </list>
    /// </summary>
    public class TimeIsOfTheEssenceQuest : MLQuest
    {
        public TimeIsOfTheEssenceQuest()
        {
            Activated = true;
            Title = 1156512;       // The Valley of One
            Description = 1156458;
            RefusalMessage = 1156460;
            InProgressMessage = 1156461;
            CompletionMessage = 1156462;

            Objectives.Add(
                new DeliverObjective(
                    typeof(KingBlackthornOrders),
                    1,
                    "приказ короля Блэкторна сэру Джеффри",
                    typeof(SirGeoffery)
                )
            );

            Rewards.Add(new DummyReward(1156459)); // A step closer to quelling the Myrmidex threat...
        }

        public override Type NextQuest => typeof(UnitingTheTribesQuest);
    }

    public class UnitingTheTribesQuest : MLQuest
    {
        public UnitingTheTribesQuest()
        {
            Activated = true;
            Title = 1156463;
            Description = 1156464;
            RefusalMessage = 1156472;
            InProgressMessage = 1156518;
            CompletionMessage = 1156519;

            Objectives.Add(new CollectObjective(1, typeof(MosaicOfHeluzz), "доверие племени Саккхра"));
            Objectives.Add(new CollectObjective(1, typeof(TotemOfFabozz), "доверие племени Урали"));
            Objectives.Add(new CollectObjective(1, typeof(FiresOfKukuzz), "доверие племени Джукари"));
            Objectives.Add(new CollectObjective(1, typeof(SkullOfMotazz), "доверие племени Курак"));
            Objectives.Add(new CollectObjective(1, typeof(SkullOfAphazz), "доверие племени Барако"));

            Rewards.Add(new ItemReward(1156581, typeof(UniqueTreasureBag), 1)); // A bag with a unique treasure
        }

        public override bool IsChainTriggered => true;
    }

    public class TheGreatHuntQuest : MLQuest
    {
        public TheGreatHuntQuest()
        {
            Activated = true;
            Title = 1156521;
            Description = 1156522;
            RefusalMessage = 1156550;
            InProgressMessage = 1156531;
            CompletionMessage = 1156557;

            Objectives.Add(new KillObjective(1, new[] { typeof(TRex) }, "тираннозавр рекс"));

            Rewards.Add(new ItemReward(1156551, typeof(MosaicOfHeluzz), 1)); // Trust of the Sakkhra Tribe
        }
    }

    public class EmptyNestQuest : MLQuest
    {
        public EmptyNestQuest()
        {
            Activated = true;
            Title = 1156523;
            Description = 1156524;
            RefusalMessage = 1156550;
            InProgressMessage = 1156536;
            CompletionMessage = 1156537;

            Objectives.Add(new KillObjective(5, new[] { typeof(Poacher) }, "браконьер"));

            Rewards.Add(new ItemReward(1156552, typeof(TotemOfFabozz), 1)); // Trust of the Urali Tribe
        }
    }

    public class TheGreatVolcanoQuest : MLQuest
    {
        public TheGreatVolcanoQuest()
        {
            Activated = true;
            Title = 1156525;
            Description = 1156526;
            RefusalMessage = 1156532;
            InProgressMessage = 1156540;
            CompletionMessage = 1156541;

            Objectives.Add(new KillObjective(5, new[] { typeof(VolcanoElemental) }, "лавовый элементал"));

            Rewards.Add(new ItemReward(1156553, typeof(FiresOfKukuzz), 1)); // Trust of the Jukari Tribe
        }
    }

    public class PrideOfTheAmbushQuest : MLQuest
    {
        public PrideOfTheAmbushQuest()
        {
            Activated = true;
            Title = 1156527;
            Description = 1156528;
            RefusalMessage = 1156532;
            InProgressMessage = 1156545;
            CompletionMessage = 1156546;

            Objectives.Add(new KillObjective(5, new[] { typeof(Trapper) }, "зверолов"));

            Rewards.Add(new ItemReward(1156554, typeof(SkullOfMotazz), 1)); // Trust of the Kurak Tribe
        }
    }

    public class TheGreatApeQuest : MLQuest
    {
        public TheGreatApeQuest()
        {
            Activated = true;
            Title = 1156529;
            Description = 1156530;
            RefusalMessage = 1156532;
            InProgressMessage = 1156548;
            CompletionMessage = 1156549;

            Objectives.Add(new KillObjective(1, new[] { typeof(GreatApe) }, "великая обезьяна"));

            Rewards.Add(new ItemReward(1156555, typeof(SkullOfAphazz), 1)); // Trust of the Barako Tribe
        }
    }
}
