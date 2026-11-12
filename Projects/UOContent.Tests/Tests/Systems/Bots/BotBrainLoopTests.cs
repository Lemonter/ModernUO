using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;
using Server.Systems.Bots;
using Xunit;

namespace Server.Tests.Systems.Bots;

[Collection("Sequential UOContent Tests")]
public class BotBrainLoopTests
{
    private sealed class FixedAction : BotAction
    {
        private readonly BotActionStatus _status;

        public FixedAction(BotActionStatus status) => _status = status;

        public override BotActionResult Tick(BotBrain brain) =>
            _status == BotActionStatus.Done ? BotActionResult.Done() : BotActionResult.Failed();

        public override string Describe(BotBrain brain) => _status.ToString();
    }

    // «Дойти» всегда удаётся, «сделать» всегда отказывает — так выглядит любая безнадёжная цель.
    private sealed class WalkThenFailGoal : BotGoal
    {
        public int Plans;

        public override string Name => "Тест: дойти и не суметь";

        public override double Score(BotBrain brain) => 0;

        public override List<BotAction> Plan(BotBrain brain)
        {
            Plans++;
            return [new FixedAction(BotActionStatus.Done), new FixedAction(BotActionStatus.Failed)];
        }
    }

    private sealed class ThrowingGoal : BotGoal
    {
        public override string Name => "Тест: падает";

        public override double Score(BotBrain brain) => throw new System.InvalidOperationException("test");

        public override List<BotAction> Plan(BotBrain brain) => throw new System.InvalidOperationException("test");
    }

    private static BotMobile NewBot()
    {
        var bot = new BotMobile { Name = "Тест", Player = true, Body = 0x190 };
        bot.AddItem(new Backpack());
        bot.MoveToWorld(new Point3D(1500, 1620, 0), Map.Felucca);
        bot.Brain = new BotBrain(bot, bot.Location, bot.Map, null);
        return bot;
    }

    [Fact]
    public void Goal_WhoseStepsAlternateDoneAndFailed_IsPausedAfterThreeRounds()
    {
        var bot = NewBot();
        var goal = new WalkThenFailGoal();

        try
        {
            var brain = bot.Brain;
            Assert.True(brain.Pursue(goal));

            for (var i = 0; i < 20 && !brain.IsPaused(goal); i++)
            {
                brain.Think();
            }

            Assert.True(brain.IsPaused(goal));
            Assert.Equal(3, goal.Plans);
        }
        finally
        {
            bot.Delete();
        }
    }

    [Fact]
    public void Goal_ThatThrowsWhilePlanning_IsPausedInsteadOfBreakingTheBrain()
    {
        var bot = NewBot();
        var goal = new ThrowingGoal();

        try
        {
            var brain = bot.Brain;
            Assert.False(brain.Pursue(goal));
            Assert.True(brain.IsPaused(goal));
        }
        finally
        {
            bot.Delete();
        }
    }
}
