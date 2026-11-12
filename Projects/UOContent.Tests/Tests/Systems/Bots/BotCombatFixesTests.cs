using Server.Items;
using Server.Mobiles;
using Server.Systems.Bots;
using Server.Targeting;
using Xunit;

namespace Server.Tests.Systems.Bots;

[Collection("Sequential UOContent Tests")]
public class BotCombatFixesTests
{
    private static BotMobile NewBot(Point3D at)
    {
        var bot = new BotMobile { Name = "Боец", Player = true, Body = 0x190 };
        bot.AddItem(new Backpack());
        bot.RawStr = 80;
        bot.RawInt = 80;
        bot.RawDex = 60;
        bot.Hits = bot.HitsMax;
        bot.Mana = bot.ManaMax;
        bot.MoveToWorld(at, Map.Felucca);
        bot.Brain = new BotBrain(bot, bot.Location, bot.Map, null);
        return bot;
    }

    private sealed class RecordingTarget : Target
    {
        public object Got;

        public RecordingTarget() : base(10, false, TargetFlags.Beneficial)
        {
        }

        protected override void OnTarget(Mobile from, object targeted) => Got = targeted;
    }

    [Fact]
    public void Warrior_WithoutSchoolSkills_NeverStartsASchoolSpell()
    {
        var bot = NewBot(new Point3D(1520, 1600, 0));
        var rat = new Rat();

        try
        {
            bot.Skills.Swords.Base = 90;
            rat.MoveToWorld(new Point3D(1521, 1600, 0), Map.Felucca);

            for (var i = 0; i < 50; i++)
            {
                Assert.False(BotSchools.TryCast(bot.Brain, SchoolUse.Buff, rat));
                Assert.False(BotSchools.TryCast(bot.Brain, SchoolUse.Heal, rat));
                Assert.False(BotSchools.TryCast(bot.Brain, SchoolUse.Attack, rat));
            }

            Assert.Null(bot.Spell);
        }
        finally
        {
            bot.Delete();
            rat.Delete();
        }
    }

    [Fact]
    public void Necromancer_WithoutReagents_DoesNotStartACast()
    {
        var bot = NewBot(new Point3D(1524, 1600, 0));
        var rat = new Rat();

        try
        {
            bot.Skills.Necromancy.Base = 100;
            rat.MoveToWorld(new Point3D(1526, 1600, 0), Map.Felucca);

            Assert.False(BotSchools.TryCast(bot.Brain, SchoolUse.Attack, rat));
            Assert.Null(bot.Spell);
        }
        finally
        {
            bot.Delete();
            rat.Delete();
        }
    }

    [Fact]
    public void HelpingCursor_GoesToTheAlly_ItWasCastFor()
    {
        var healer = NewBot(new Point3D(1528, 1600, 0));
        var patient = NewBot(new Point3D(1529, 1600, 0));

        try
        {
            var cursor = new RecordingTarget();
            healer.Target = cursor;
            healer.Brain.Combat.SpellTarget = patient;
            healer.Brain.Combat.SpellForAlly = true;

            BotCombat.Think(healer.Brain);
            Assert.Same(patient, cursor.Got);

            // Without the ally mark (a mastery passing the foe) a helping cursor stays on the bot.
            cursor = new RecordingTarget();
            healer.Target = cursor;
            healer.Brain.Combat.SpellTarget = patient;

            BotCombat.Think(healer.Brain);
            Assert.Same(healer, cursor.Got);
        }
        finally
        {
            healer.Delete();
            patient.Delete();
        }
    }

    [Fact]
    public void Archer_WithoutArrows_PutsTheBowAway_AndDoesNotHunt()
    {
        var bot = NewBot(new Point3D(1532, 1600, 0));
        var rat = new Rat();

        try
        {
            bot.Skills.Archery.Base = 80;
            bot.Skills.Fencing.Base = 50;
            bot.Skills.Tactics.Base = 80;
            var bow = new Bow();
            bot.AddItem(bow);
            // В тестовом хосте нет tiledata, слоя у предметов нет сам по себе.
            var dagger = new Dagger { Layer = Layer.OneHanded };
            bot.Backpack.DropItem(dagger);

            Assert.False(BotCombatStyles.IsArmed(bot));
            Assert.Equal(0, BotGoals.Hunt.Score(bot.Brain));

            rat.MoveToWorld(new Point3D(1533, 1600, 0), Map.Felucca);
            rat.DoHarmful(bot);
            BotCombat.Think(bot.Brain);

            Assert.Same(bot.Backpack, bow.Parent);
            Assert.Same(dagger, bot.Weapon);
            Assert.Same(bot.Backpack, bow.Parent);
        }
        finally
        {
            bot.Delete();
            rat.Delete();
        }
    }

    [Fact]
    public void Archer_WithArrows_KeepsTheBow()
    {
        var bot = NewBot(new Point3D(1536, 1600, 0));

        try
        {
            bot.Skills.Archery.Base = 80;
            bot.Skills.Tactics.Base = 80;
            bot.AddItem(new Bow());
            bot.Backpack.DropItem(new Arrow(50));

            Assert.True(BotCombatStyles.IsArmed(bot));
        }
        finally
        {
            bot.Delete();
        }
    }
}
