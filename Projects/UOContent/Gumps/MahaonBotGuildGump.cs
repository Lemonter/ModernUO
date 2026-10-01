using Server.Items;
using Server.Network;
using Server.Systems.MahaonBots;

namespace Server.Gumps;

public class MahaonBotGuildGump : StaticGump<MahaonBotGuildGump>
{
    private readonly Items.MahaonBotBeacon _beacon;

    public override bool Singleton => true;
    protected override bool Cached => false;

    public MahaonBotGuildGump(Items.MahaonBotBeacon beacon) : base(80, 80) => _beacon = beacon;

    protected override void BuildLayout(ref StaticGumpBuilder builder)
    {
        var names = BotGuilds.NamePool;
        var myGuildName = _beacon.GuildName;
        var haveGuild = !string.IsNullOrEmpty(myGuildName);

        // Must mirror the real layout below exactly — the background/alpha region are added
        // before the content that determines how tall they should be. Was a flat guess
        // (150 / 310+list) that never accounted for the guild-picker list at all in the
        // no-guild case, or the "relation to others" section (added later) in the haveGuild
        // case — with the full 15-name roster the real content ran ~300-400px past the
        // background, clipping/overlapping the back button and half the relations list.
        const int HeaderHeight = 90;               // title + status line + "no guild" button
        const int GuildInfoBlockHeight = 124;       // member/pvp/crafts/training-header/boost-button
        const int PickerRowHeight = 24;
        const int RelationsHeaderHeight = 10 + 24;  // gap + "Отношение..." header
        const int BottomMargin = 40;

        var height = HeaderHeight + (haveGuild ? GuildInfoBlockHeight : 0) + names.Length * PickerRowHeight;

        if (haveGuild)
        {
            height += RelationsHeaderHeight + (names.Length - 1) * PickerRowHeight;
        }

        height += BottomMargin;

        builder.AddPage();
        builder.AddBackground(0, 0, 360, height, 5054);
        builder.AddAlphaRegion(10, 10, 340, height - 20);

        builder.AddHtml(20, 15, 320, 20, "Гильдия ботов этого маяка");
        builder.AddHtml(
            20, 38, 320, 20,
            haveGuild ? $"Сейчас: {myGuildName}" : "Сейчас: без гильдии"
        );

        builder.AddButton(20, 62, 4017, 4019, 9);
        builder.AddHtml(55, 64, 200, 20, "Без гильдии");

        var y = 90;

        if (haveGuild)
        {
            var guild = BotGuilds.TryGet(myGuildName);
            var memberCount = guild?.Members.Count ?? 0;
            var goldValue = GuildBank.GetGoldValue(myGuildName);
            var gemCount = GuildBank.GetGemCount(myGuildName);
            var pvpPercent = GuildSpecialization.GetPvpPercent(myGuildName);
            var allowsCrafts = GuildSpecialization.GetAllowsCrafts(myGuildName);

            builder.AddHtml(20, y, 320, 20, $"Членов: {memberCount}   Золота в банке: {goldValue}   Камней: {gemCount}");
            y += 24;

            builder.AddHtml(20, y, 150, 20, $"PvP-направленность: {pvpPercent}%");
            builder.AddButton(180, y, 4020, 4022, 200); // -10
            builder.AddHtml(205, y + 2, 20, 20, "-");
            builder.AddButton(230, y, 4020, 4022, 201); // +10
            builder.AddHtml(255, y + 2, 20, 20, "+");
            y += 24;

            builder.AddButton(20, y, allowsCrafts ? 4006 : 4005, allowsCrafts ? 4008 : 4007, 202);
            builder.AddHtml(55, y + 2, 250, 20, allowsCrafts ? "Крафты разрешены" : "Крафты запрещены (клик — включить)");
            y += 30;

            var purchaseCount = GuildUpgrades.GetPurchaseCount(myGuildName);
            var nextCost = GuildUpgrades.GetNextPurchaseCost(myGuildName);

            builder.AddHtml(
                20, y, 320, 20,
                $"Боевая подготовка: {purchaseCount}/{GuildUpgrades.MaxLevel} — усиливает одного случайного живого члена гильдии на +{GuildUpgrades.StatBonusPerPurchase} к статам"
            );
            y += 22;

            if (nextCost >= 0)
            {
                var canAfford = gemCount >= nextCost;
                builder.AddButton(20, y, canAfford ? 4005 : 4014, canAfford ? 4007 : 4016, canAfford ? 203 : 0);
                builder.AddHtml(55, y + 2, 280, 20, $"Усилить случайного бойца за {nextCost} камней");
            }
            else
            {
                builder.AddHtml(20, y, 280, 20, "Все усиления уже куплены");
            }

            y += 24;
        }

        for (var i = 0; i < names.Length; i++)
        {
            var isCurrent = names[i] == myGuildName;
            builder.AddButton(20, y, isCurrent ? 4006 : 4005, isCurrent ? 4008 : 4007, 10 + i);
            builder.AddHtml(55, y + 2, 280, 20, isCurrent ? $"» {names[i]}" : names[i]);
            y += 24;
        }

        if (haveGuild)
        {
            y += 10;
            builder.AddHtml(20, y, 320, 20, $"Отношение «{myGuildName}» к другим гильдиям:");
            y += 24;

            for (var i = 0; i < names.Length; i++)
            {
                if (names[i] == myGuildName)
                {
                    continue;
                }

                var relation = BotGuilds.GetRelation(myGuildName, names[i]);

                var label = relation switch
                {
                    BotGuildRelation.War   => "[Война]",
                    BotGuildRelation.Ally  => "[Союз]",
                    _                       => "[Нейтрально]"
                };

                builder.AddHtml(20, y + 2, 150, 20, $"{names[i]} {label}");

                builder.AddButton(180, y, 4020, 4022, 100 + i * 3 + (int)BotGuildRelation.War);
                builder.AddHtml(205, y + 2, 45, 20, "Война");

                builder.AddButton(255, y, 4020, 4022, 100 + i * 3 + (int)BotGuildRelation.Ally);
                builder.AddHtml(280, y + 2, 45, 20, "Союз");

                builder.AddButton(330, y, 4020, 4022, 100 + i * 3 + (int)BotGuildRelation.Neutral);
                y += 24;
            }
        }

        builder.AddButton(20, height - 30, 4014, 4016, 1);
        builder.AddHtml(55, height - 28, 150, 20, "Назад к маяку");
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (_beacon.Deleted)
        {
            return;
        }

        var names = BotGuilds.NamePool;

        if (info.ButtonID == 1)
        {
            sender.Mobile?.SendGump(new MahaonBotBeaconGump(_beacon));
            return;
        }

        if (info.ButtonID == 9)
        {
            _beacon.GuildName = null;
            sender.Mobile?.SendGump(new MahaonBotGuildGump(_beacon));
            return;
        }

        if (info.ButtonID is >= 10 and < 10 + 32)
        {
            var index = info.ButtonID - 10;
            if (index < names.Length)
            {
                _beacon.GuildName = names[index];
            }

            sender.Mobile?.SendGump(new MahaonBotGuildGump(_beacon));
            return;
        }

        if (info.ButtonID >= 100 && info.ButtonID < 200 && !string.IsNullOrEmpty(_beacon.GuildName))
        {
            var offset = info.ButtonID - 100;
            var otherIndex = offset / 3;
            var relationCode = offset % 3;

            if (otherIndex >= 0 && otherIndex < names.Length && names[otherIndex] != _beacon.GuildName)
            {
                BotGuilds.SetRelation(_beacon.GuildName, names[otherIndex], (BotGuildRelation)relationCode);
            }

            sender.Mobile?.SendGump(new MahaonBotGuildGump(_beacon));
            return;
        }

        if (!string.IsNullOrEmpty(_beacon.GuildName))
        {
            switch (info.ButtonID)
            {
                case 200: // PvP -10
                    GuildSpecialization.SetPvpPercent(
                        _beacon.GuildName, GuildSpecialization.GetPvpPercent(_beacon.GuildName) - 10
                    );
                    sender.Mobile?.SendGump(new MahaonBotGuildGump(_beacon));
                    return;

                case 201: // PvP +10
                    GuildSpecialization.SetPvpPercent(
                        _beacon.GuildName, GuildSpecialization.GetPvpPercent(_beacon.GuildName) + 10
                    );
                    sender.Mobile?.SendGump(new MahaonBotGuildGump(_beacon));
                    return;

                case 202: // Toggle crafts
                    GuildSpecialization.SetAllowsCrafts(
                        _beacon.GuildName, !GuildSpecialization.GetAllowsCrafts(_beacon.GuildName)
                    );
                    sender.Mobile?.SendGump(new MahaonBotGuildGump(_beacon));
                    return;

                case 203: // Buy one boost, applied to a random living guild member
                    if (!GuildUpgrades.TryBoostRandomMember(_beacon.GuildName))
                    {
                        sender.Mobile?.SendMessage(0x22, "Не удалось усилить бойца — не хватает камней или в гильдии нет живых членов.");
                    }

                    sender.Mobile?.SendGump(new MahaonBotGuildGump(_beacon));
                    return;
            }
        }
    }
}
