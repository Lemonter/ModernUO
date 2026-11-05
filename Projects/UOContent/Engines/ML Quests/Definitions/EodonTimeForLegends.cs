using Server.Engines.MLQuests.Objectives;
using Server.Engines.MLQuests.Rewards;
using Server.Items;
using Server.Mobiles;
using Server.Spells.SkillMasteries;

namespace Server.Engines.MLQuests.Definitions
{
    /// <summary>
    ///     Hawkwind's "A Time For Legends", ported from ServUO (Scripts/Quests/Eodon/Hawkwind/).
    ///
    ///     Dropped: <c>ChooseMasteryGump</c> and the per-day <c>TargetOfTheDay</c> rotation. MLQuest objectives are
    ///     fixed at definition time, so the target list is the whole ServUO legendary-target table and the player may
    ///     defeat any one of them; the mastery choice gump is replaced by handing out a primer for the first mastery
    ///     the player has not learned yet.
    /// </summary>
    public class TimeForLegendsQuest : MLQuest
    {
        private static readonly System.Type[] _targets =
        {
            typeof(Semidar), typeof(Mephitis), typeof(Rikktor), typeof(LordOaks), typeof(Neira), typeof(Barracoon),
            typeof(Serado), typeof(Meraktus), typeof(Ilhenir), typeof(DreadHorn), typeof(Travesty),
            typeof(ChiefParoxysmus), typeof(LadyMelisande), typeof(MonstrousInterredGrizzle), typeof(ShimmeringEffusion)
        };

        public TimeForLegendsQuest()
        {
            Activated = true;
            Title = 1156338;       // A Time For Legends
            Description = 1156339;
            RefusalMessage = 1156339;
            InProgressMessage = 1156341;
            CompletionMessage = 1156342;

            Objectives.Add(new KillObjective(1, _targets, "легендарное чудовище"));

            Rewards.Add(new ItemReward("книга мастерства", typeof(BookOfMasteries), 1));
        }

        public override void GetRewards(MLQuestInstance instance)
        {
            var pm = instance.Player;

            foreach (var skill in MasteryInfo.Skills)
            {
                if (MasteryInfo.HasLearned(pm, skill))
                {
                    continue;
                }

                if (pm.PlaceInBackpack(new SkillMasteryPrimer(skill, 1)))
                {
                    pm.SendLocalizedMessage(1156209); // You have received a mastery primer!
                    break;
                }
            }

            base.GetRewards(instance);
        }
    }
}
