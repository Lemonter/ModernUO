using System.Collections.Generic;
using Server.Guilds;
using Server.Items;
using Server.Mobiles;
using Server.Systems.MahaonBots;
using Server.Systems.MahaonCities;

namespace Server.Systems.Bots;

public partial class BotBrain
{
    // A member who gave to the guild bank gives again no sooner than this tick.
    internal long NextDonationTick;
}

/// <summary>The cities a bot's guild holds and what their guard needs next.</summary>
public static class BotCityGuard
{
    /// <summary>The held city whose next guard step is cheapest, with that step.</summary>
    public static (string city, CityControlSystem.GuardStep step)? NextStep(Guild guild)
    {
        (string, CityControlSystem.GuardStep)? best = null;
        foreach (var city in CityControlSystem.Cities.Keys)
        {
            if (CityControlSystem.GetController(city) == guild && CityControlSystem.NextStep(city) is { } step &&
                (best == null || step.Gold < best.Value.Item2.Gold))
            {
                best = (city, step);
            }
        }

        return best;
    }

    /// <summary>A held city with room for another battle mage.</summary>
    public static string CityWantingMage(Guild guild)
    {
        foreach (var city in CityControlSystem.Cities.Keys)
        {
            if (CityControlSystem.GetController(city) == guild && CityControlSystem.GetMageSlots(city) < CityControlSystem.MaxMages)
            {
                return city;
            }
        }

        return null;
    }
}

/// <summary>
/// The leader of a guild holding a city spends the guild bank on its guard: the next step when the
/// bank holds the gold and the ingots; a battle mage, then teachers, from gold the next step won't
/// need. Through the city stone's buttons a player's leader presses.
/// </summary>
public sealed class CityGuardUpgradeGoal : BotGoal
{
    public override string Name => "Стража города";

    public override string[] News => ["Подняли стражу нашего города, теперь не сунутся.", "Наняли в город боевого мага.", "Нашли страже хорошего учителя."];

    private static CityGuardCommandAction Choice(PlayerMobile bot)
    {
        if (bot.Guild is not Guild guild || guild.Leader != bot)
        {
            return null;
        }

        var gold = GuildBank.GetGoldValue(guild.Name);

        if (BotCityGuard.NextStep(guild) is { } next && gold >= next.step.Gold &&
            GuildBank.GetIngots(guild.Name, next.step.Metal) >= next.step.Ingots)
        {
            return new CityGuardCommandAction(next.city, GuardTeachers.None, false);
        }

        var reserve = BotCityGuard.NextStep(guild)?.step.Gold ?? 0;
        if (BotCityGuard.CityWantingMage(guild) is { } mageCity && gold >= CityControlSystem.MageCost(mageCity) + reserve)
        {
            return new CityGuardCommandAction(mageCity, GuardTeachers.None, true);
        }

        foreach (var city in CityControlSystem.Cities.Keys)
        {
            if (CityControlSystem.GetController(city) != guild || gold < CityControlSystem.TeacherCost(city) + reserve)
            {
                continue;
            }

            foreach (var (teacher, _) in Gumps.CityStoneGump.TeacherList)
            {
                if ((CityControlSystem.TeachersOf(city) & teacher) == 0)
                {
                    return new CityGuardCommandAction(city, teacher, false);
                }
            }
        }

        return null;
    }

    public override double Score(BotBrain brain) => Choice(brain.Bot) == null ? 0 : 0.6;

    public override List<BotAction> Plan(BotBrain brain) => Choice(brain.Bot) is { } action ? [action] : null;
}

/// <summary>One press of a city stone's button: the next guard step, a battle mage or a teacher.</summary>
public sealed class CityGuardCommandAction : BotAction
{
    private readonly string _city;
    private readonly GuardTeachers _teacher;
    private readonly bool _mage;

    public CityGuardCommandAction(string city, GuardTeachers teacher, bool mage)
    {
        _city = city;
        _teacher = teacher;
        _mage = mage;
    }

    public override BotActionResult Tick(BotBrain brain)
    {
        var bot = brain.Bot;
        if (bot.Guild is not Guild guild)
        {
            return BotActionResult.Failed();
        }

        if (_teacher != GuardTeachers.None)
        {
            if (!CityControlSystem.HireTeacher(_city, guild, _teacher))
            {
                return BotActionResult.Failed();
            }

            BotSpeech.SayText(bot, $"Нанял страже {_city} учителя.");
        }
        else if (_mage)
        {
            if (!CityControlSystem.HireMage(_city, guild))
            {
                return BotActionResult.Failed();
            }

            BotSpeech.SayText(bot, $"В {_city} теперь служит ещё один боевой маг.");
        }
        else
        {
            if (!CityControlSystem.UpgradeGuards(_city, guild))
            {
                return BotActionResult.Failed();
            }

            BotSpeech.SayText(bot, $"Стража {_city} теперь {CityControlSystem.GetGuardLevel(_city)} уровня!");
        }

        return BotActionResult.Done(1000);
    }

    public override string Describe(BotBrain brain) => $"Заботится о страже {_city}";
}

/// <summary>
/// Members of a guild holding a city chip in for its guard: ingots of the metal the next step needs,
/// which they don't keep for their own craft, and a share of their gold when the bank is short of
/// it — the less greedy, the readier. Through the treasury's deposit cursor, as a player gives.
/// </summary>
public sealed class GuildDonateGoal : BotGoal
{
    // Gold a member keeps before it gives any; GuildDonateAction gives a share of the rest.
    private const int KeepGold = 30_000;

    public override string Name => "Взнос в казну";

    public override string[] News => ["Внёс в казну гильдии на стражу.", "Отдал гильдии слитки — пусть куют стражам доспехи."];

    private static Item Gift(BotBrain brain)
    {
        var bot = brain.Bot;
        if (Core.TickCount - brain.NextDonationTick < 0 || bot.Guild is not Guild guild || bot.Backpack is not { } pack ||
            BotCityGuard.NextStep(guild) is not { } next)
        {
            return null;
        }

        if (GuildBank.GetIngots(guild.Name, next.step.Metal) < next.step.Ingots)
        {
            foreach (var ingot in pack.FindItemsByType<MahaonIngot>())
            {
                if (ingot.Metal == next.step.Metal && !BotCrafting.KeepsForCraft(bot, ingot))
                {
                    return ingot;
                }
            }
        }

        if (GuildBank.GetGoldValue(guild.Name) < next.step.Gold && pack.GetAmount(typeof(Gold)) > KeepGold &&
            BotBrain.Trait(brain.Greed) < 0.7)
        {
            return pack.FindItemByType<Gold>();
        }

        return null;
    }

    public override double Score(BotBrain brain) =>
        Gift(brain) == null ? 0 : 0.35 + BotBrain.Trait((byte)(100 - brain.Greed)) * 0.3;

    public override List<BotAction> Plan(BotBrain brain) => Gift(brain) == null ? null : [new GuildDonateAction()];
}

public sealed class GuildDonateAction : BotAction
{
    private const long DonationCooldownMs = 30 * 60_000;
    private const int KeepGold = 30_000;
    private const double GoldShare = 0.2;

    public override BotActionResult Tick(BotBrain brain)
    {
        var bot = brain.Bot;
        if (bot.Guild is not Guild guild || bot.Backpack is not { } pack || BotCityGuard.NextStep(guild) is not { } next)
        {
            return BotActionResult.Failed();
        }

        Item gift = null;
        foreach (var ingot in pack.FindItemsByType<MahaonIngot>())
        {
            if (ingot.Metal == next.step.Metal && !BotCrafting.KeepsForCraft(bot, ingot))
            {
                gift = ingot;
                break;
            }
        }

        // Gold is given as its own pile: a share of what lies above what the bot keeps.
        if (gift == null && pack.GetAmount(typeof(Gold)) is var purse && purse > KeepGold)
        {
            var amount = (int)((purse - KeepGold) * GoldShare);
            if (amount > 0 && pack.ConsumeTotal(typeof(Gold), amount))
            {
                gift = new Gold(amount);
                pack.DropItem(gift);
            }
        }

        if (gift == null)
        {
            return BotActionResult.Failed();
        }

        // The treasury's "put in" button raises this cursor.
        bot.Target = new CityControlSystem.GuildDepositTarget();
        bot.Target.Invoke(bot, gift);
        brain.NextDonationTick = Core.TickCount + DonationCooldownMs;
        BotSpeech.SayText(bot, $"Это на стражу, гильдия {guild.Name}.");
        return gift.Deleted || gift.Parent != pack ? BotActionResult.Done(1000) : BotActionResult.Failed();
    }

    public override string Describe(BotBrain brain) => "Делает взнос в казну гильдии";
}
