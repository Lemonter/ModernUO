using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Systems.MahaonBots;

/// <summary>
///     [BecomeBot's UI front end — archetype pick, then goal pick. Two small steps instead
///     of typed command args so any player (not just a GM comfortable with the command
///     line) can drive it. See BotPossession.cs for the command itself and Activate().
/// </summary>
public class BotArchetypeGump : DynamicGump
{
    private readonly PlayerMobile _player;

    private static readonly (BotArchetype archetype, string label)[] Archetypes =
    {
        (BotArchetype.Warrior, "Воин — дерётся, ходит в подземелья"),
        (BotArchetype.Mage, "Маг — колдует, кайтит"),
        (BotArchetype.Archer, "Лучник — стреляет, кайтит"),
        (BotArchetype.Crafter, "Ремесленник — добывает, крафтит"),
        (BotArchetype.Trader, "Торговец — торгует, путешествует")
    };

    public BotArchetypeGump(PlayerMobile player) : base(50, 50) => _player = player;

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        var height = 60 + Archetypes.Length * 25;

        builder.AddPage();
        builder.AddBackground(0, 0, 320, height, 5054);
        builder.AddAlphaRegion(10, 10, 300, height - 20);
        builder.AddHtml(15, 15, 290, 20, "ИИ-контроль: выбери архетип персонажа");

        for (var i = 0; i < Archetypes.Length; i++)
        {
            var y = 45 + i * 25;
            builder.AddButton(15, y, 4005, 4007, i + 1);
            builder.AddHtml(45, y, 260, 20, Archetypes[i].label);
        }
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var index = info.ButtonID - 1;
        if (index < 0 || index >= Archetypes.Length)
        {
            return;
        }

        BotPossession.Activate(_player, Archetypes[index].archetype);
        _player.SendGump(new BotGoalGump(_player));
    }
}

public class BotGoalGump : DynamicGump
{
    private readonly PlayerMobile _player;

    public BotGoalGump(PlayerMobile player) : base(50, 50) => _player = player;

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        builder.AddPage();
        builder.AddBackground(0, 0, 320, 260, 5054);
        builder.AddAlphaRegion(10, 10, 300, 240);
        builder.AddHtml(15, 15, 290, 20, "Чем сейчас заняться?");

        builder.AddButton(15, 45, 4005, 4007, 1);
        builder.AddHtml(45, 45, 260, 20, "Копать руду");

        builder.AddButton(15, 70, 4005, 4007, 2);
        builder.AddHtml(45, 70, 260, 20, "Рубить дерево");

        builder.AddButton(15, 95, 4005, 4007, 3);
        builder.AddHtml(45, 95, 260, 20, "Рыбачить");

        builder.AddButton(15, 120, 4005, 4007, 4);
        builder.AddHtml(45, 120, 260, 20, "Пойти побить нпс (охота)");

        builder.AddButton(15, 145, 4005, 4007, 5);
        builder.AddHtml(45, 145, 260, 20, "Решать самому (по архетипу)");

        builder.AddButton(15, 175, 4005, 4007, 6);
        builder.AddHtml(45, 175, 260, 20, "Выключить ИИ-контроль");

        builder.AddHtml(15, 205, 290, 45, "Цель можно сменить в любой момент — снова вызови [BecomeBot. Продолжит работать, даже если отключишься от игры.");
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        switch (info.ButtonID)
        {
            case 1:
                BotController.SetGatherGoal(_player, SkillName.Mining);
                break;
            case 2:
                BotController.SetGatherGoal(_player, SkillName.Lumberjacking);
                break;
            case 3:
                BotController.SetGatherGoal(_player, SkillName.Fishing);
                break;
            case 4:
                BotController.SetHuntGoal(_player);
                break;
            case 6:
                BotController.UnregisterBot(_player);
                _player.SendMessage(0x59, "ИИ-контроль выключен — снова управляете сами.");
                break;
            // 5 (или закрытие гампа) — ничего не делаем, обычная рулетка DoIdle подхватит сама.
        }
    }
}
