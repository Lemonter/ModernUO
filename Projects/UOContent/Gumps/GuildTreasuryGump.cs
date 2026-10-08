using Server.Guilds;
using Server.Items;
using Server.Network;
using Server.Systems.MahaonBots;
using Server.Systems.MahaonCities;
using Server.Systems.MahaonMetals;

namespace Server.Gumps;

/// <summary>
/// A guild's bank as its members see it, at a banker: the gold and the ingots of each metal in it.
/// Any member puts gold, ingots and guard supplies in; the leader also takes gold out.
/// </summary>
public class GuildTreasuryGump : DynamicGump
{
    private const int Width = 360;

    private readonly Mobile _viewer;
    private readonly Guild _guild;

    public override bool Singleton => true;

    private GuildTreasuryGump(Mobile viewer, Guild guild) : base(80, 80)
    {
        _viewer = viewer;
        _guild = guild;
    }

    public static void DisplayTo(Mobile from)
    {
        if (from.Guild is not Guild guild)
        {
            from.SendMessage("Ты не состоишь в гильдии.");
            return;
        }

        from.SendGump(new GuildTreasuryGump(from, guild));
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        var leader = _guild.Leader == _viewer;
        var metals = 0;
        foreach (var (metal, _) in MahaonMetalTable.Data)
        {
            if (GuildBank.GetIngots(_guild.Name, metal) > 0)
            {
                metals++;
            }
        }

        var height = 150 + metals * 20 + (leader ? 40 : 0);

        builder.AddPage();
        builder.AddBackground(0, 0, Width, height, 5054);
        builder.AddAlphaRegion(10, 10, Width - 20, height - 20);
        builder.AddHtml(20, 18, Width - 40, 20, $"<center>Казна гильдии {_guild.Name}</center>");
        builder.AddHtml(20, 45, Width - 40, 20, $"Золото: {GuildBank.GetGoldValue(_guild.Name)}");

        var y = 70;
        foreach (var (metal, info) in MahaonMetalTable.Data)
        {
            var ingots = GuildBank.GetIngots(_guild.Name, metal);
            if (ingots > 0)
            {
                builder.AddHtml(20, y, Width - 40, 20, $"Слитки ({info.RuName}): {ingots}");
                y += 20;
            }
        }

        y += 10;
        builder.AddButton(20, y, 4005, 4007, 1);
        builder.AddHtml(55, y, Width - 75, 20, "Внести золото, слитки, бинты, зелья или свитки");
        y += 30;

        if (leader)
        {
            builder.AddButton(20, y, 4005, 4007, 2);
            builder.AddHtml(55, y, 120, 20, "Снять золото:");
            builder.AddBackground(180, y - 2, 120, 24, 9350);
            builder.AddTextEntry(185, y, 110, 20, 0, 0, "");
        }
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var from = sender.Mobile;
        if (from.Guild != _guild)
        {
            return;
        }

        switch (info.ButtonID)
        {
            case 1:
                {
                    from.SendMessage("Что положить в казну гильдии?");
                    from.Target = new CityControlSystem.GuildDepositTarget();
                    break;
                }
            case 2 when _guild.Leader == from:
                {
                    if (!int.TryParse(info.GetTextEntry(0)?.Trim(), out var amount) || amount <= 0)
                    {
                        from.SendMessage("Сколько золота снять? Впиши число.");
                    }
                    else if (!GuildBank.TrySpend(_guild.Name, amount, MahaonMetal.Iron, 0))
                    {
                        from.SendMessage(0x22, "В казне столько нет.");
                    }
                    else if (!Mobiles.Banker.Deposit(from, amount))
                    {
                        // The bank box had no room: the gold goes back to the guild.
                        GuildBank.DepositGold(_guild.Name, amount);
                        from.SendMessage(0x22, "В твоём банке нет места.");
                    }
                    else
                    {
                        from.SendMessage(0x59, $"{amount} золота из казны гильдии переведено в твой банк.");
                    }

                    DisplayTo(from);
                    break;
                }
        }
    }
}
