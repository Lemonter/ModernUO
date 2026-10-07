using Server.Guilds;
using Server.Items;
using Server.Network;
using Server.Systems.MahaonBots;
using Server.Systems.MahaonCities;
using Server.Systems.MahaonMetals;

namespace Server.Gumps;

/// <summary>
/// A city's stone: who holds the city and how it is kept. A guild leader claims the city here; the
/// leader of the holding guild sets the tax, raises the guard and hires battle mages from it.
/// </summary>
public class CityStoneGump : DynamicGump
{
    private const int Width = 440;
    private const int UseRange = 3;

    private readonly MahaonCityClaimPoint _stone;
    private readonly Mobile _viewer;

    public override bool Singleton => true;

    private CityStoneGump(Mobile viewer, MahaonCityClaimPoint stone) : base(60, 60)
    {
        _viewer = viewer;
        _stone = stone;
    }

    public static void DisplayTo(Mobile from, MahaonCityClaimPoint stone)
    {
        if (stone?.Deleted != false || stone.City == null || !CityControlSystem.Cities.ContainsKey(stone.City))
        {
            from.SendMessage("Камень не привязан к городу.");
            return;
        }

        if (!from.InRange(stone.GetWorldLocation(), UseRange))
        {
            from.SendLocalizedMessage(500446); // That is too far away.
            return;
        }

        from.SendGump(new CityStoneGump(from, stone));
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        var from = _viewer;
        var city = _stone.City;
        var holder = CityControlSystem.GetController(city);
        var rules = CityControlSystem.Rules(from, city);
        var height = rules ? 410 : 200;

        builder.AddPage();
        builder.AddBackground(0, 0, Width, height, 5054);
        builder.AddAlphaRegion(10, 10, Width - 20, height - 20);
        builder.AddHtml(20, 18, Width - 40, 20, $"<center>Городской камень: {city}</center>");

        if (holder == null)
        {
            builder.AddHtml(20, 45, Width - 40, 20, "Город никому не принадлежит.");
        }
        else
        {
            builder.AddHtml(20, 45, Width - 40, 20, $"Город держит гильдия {holder.Name}. Налог {CityControlSystem.GetTaxRate(city)}%.");
            builder.AddHtml(
                20, 67, Width - 40, 40,
                $"Стража: ступень {CityControlSystem.GetGuardLevel(city)}, боевых магов {CityControlSystem.MagesIn(city)} из {CityControlSystem.GetMageSlots(city)}, стражников {CityControlSystem.SwordGuardsIn(city)} из {CityControlSystem.GuardRoster(city)}."
            );
        }

        if (_stone.Contested)
        {
            builder.AddHtml(20, 112, Width - 40, 30, $"Город оспаривает гильдия {_stone.Contender.Name}!");
        }
        else if (from.Guild is Guild guild && guild.Leader == from && holder != guild)
        {
            builder.AddButton(20, 112, 4005, 4007, 1);
            builder.AddHtml(55, 112, Width - 75, 30, "Заявить права на город — продержаться у камня десять минут");
        }

        if (rules)
        {
            BuildRule(ref builder, city, holder);
        }

        builder.AddButton(20, height - 40, 4017, 4019, 0);
        builder.AddHtml(55, height - 40, 100, 20, "Закрыть");
    }

    private static void BuildRule(ref DynamicGumpBuilder builder, string city, Guild holder)
    {
        var gold = GuildBank.GetGoldValue(holder.Name);
        builder.AddHtml(20, 145, Width - 40, 20, $"<basefont color=#FFD700>Управление городом</basefont> · казна: {gold} золота");

        builder.AddHtml(20, 170, 160, 20, $"Налог: {CityControlSystem.GetTaxRate(city)}%");
        builder.AddButton(190, 170, 5603, 5607, 2); // less
        builder.AddButton(215, 170, 5601, 5605, 3); // more

        if (CityControlSystem.NextStep(city) is { } step)
        {
            var metal = MahaonMetalTable.Get(step.Metal).RuName;
            var have = GuildBank.GetIngots(holder.Name, step.Metal);
            builder.AddButton(20, 200, 4005, 4007, 4);
            builder.AddHtml(
                55, 200, Width - 75, 40,
                $"Поднять стражу до ступени {CityControlSystem.GetGuardLevel(city) + 1}: {step.Gold} золота и {step.Ingots} слитков ({metal}), в казне {have}"
            );
        }
        else
        {
            builder.AddHtml(20, 200, Width - 40, 20, "Стража на высшей ступени.");
        }

        if (CityControlSystem.GetMageSlots(city) < CityControlSystem.MaxMages)
        {
            builder.AddButton(20, 245, 4005, 4007, 5);
            builder.AddHtml(55, 245, Width - 75, 20, $"Нанять боевого мага: {CityControlSystem.MageCost(city)} золота");
        }
        else
        {
            builder.AddHtml(20, 245, Width - 40, 20, $"Боевых магов полный набор: {CityControlSystem.MaxMages}.");
        }

        var level = CityControlSystem.GetGuardLevel(city);
        var (replaceGold, replaceIngots) = CityGuardUpkeep.ReplaceCost(level);
        builder.AddHtml(
            20, 275, Width - 40, 40,
            $"Жалованье: {CityGuardUpkeep.WagePerHour(level)} золота в час на стражника. Замена павшего: {replaceGold} золота и {replaceIngots} слитков."
        );

        builder.AddButton(20, 320, 4005, 4007, 6);
        builder.AddHtml(55, 320, Width - 75, 20, "Казна гильдии");
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var from = sender.Mobile;
        if (info.ButtonID == 0 || _stone.Deleted || !from.InRange(_stone.GetWorldLocation(), UseRange))
        {
            return;
        }

        var city = _stone.City;

        switch (info.ButtonID)
        {
            case 1:
                {
                    if (CityControlSystem.TryClaim(from, _stone) is { } why)
                    {
                        from.SendMessage(0x22, why);
                    }
                    else
                    {
                        from.SendMessage(0x59, "Права заявлены. Продержитесь у камня десять минут.");
                    }

                    break;
                }
            case 2:
            case 3:
                {
                    var step = info.ButtonID == 2 ? -5 : 5;
                    CityControlSystem.SetTax(city, from, CityControlSystem.GetTaxRate(city) + step);
                    break;
                }
            case 4:
                {
                    if (CityControlSystem.Rules(from, city) && !CityControlSystem.UpgradeGuards(city, (Guild)from.Guild))
                    {
                        from.SendMessage(0x22, "В казне не хватает золота или слитков на следующую ступень.");
                    }

                    break;
                }
            case 5:
                {
                    if (CityControlSystem.Rules(from, city) && !CityControlSystem.HireMage(city, (Guild)from.Guild))
                    {
                        from.SendMessage(0x22, "В казне не хватает золота на мага.");
                    }

                    break;
                }
            case 6:
                {
                    GuildTreasuryGump.DisplayTo(from);
                    return;
                }
        }

        DisplayTo(from, _stone);
    }
}
