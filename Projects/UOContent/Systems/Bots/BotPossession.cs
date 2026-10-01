using Server.Commands;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Systems.Bots;

/// <summary>
/// [BecomeBot — hands the caller's own character to the bot brain: an AFK mode (the character
/// keeps working after the client disconnects, see PlayerMobile.GetLogoutDelay) and a way to
/// watch the real decision loop from the inside. Open to players. The same brain, goals and
/// combat drive it as any spawned bot; death and resurrection go through the ghost walk.
/// </summary>
public static class BotPossession
{
    public static void Configure()
    {
        CommandSystem.Register("BecomeBot", AccessLevel.Player, OnBecomeBot);
    }

    [Usage("BecomeBot")]
    [Description("Открывает гамп ИИ-контроля над своим персонажем — AFK-режим и отладка одновременно.")]
    private static void OnBecomeBot(CommandEventArgs e)
    {
        if (e.Mobile is PlayerMobile player and not BotMobile)
        {
            player.SendGump(new BotGoalGump(player));
        }
    }

    public static void Engage(PlayerMobile player, BotGoal focus)
    {
        var fresh = !BotSystem.IsPossessed(player);
        var brain = BotSystem.Possess(player);
        if (brain == null)
        {
            return;
        }

        brain.Focus = focus;
        brain.ClearPlan();

        if (fresh)
        {
            player.SendMessage(
                0x59,
                "ИИ-контроль включён: персонаж действует сам — работает, торгует, охотится, лечится и дерётся. " +
                "Продолжит, даже если отключишься. Сменить цель или выключить — снова [BecomeBot."
            );
        }
        else
        {
            player.SendMessage(0x59, $"Новая цель: {focus?.Name ?? "по собственному разумению"}.");
        }
    }

    public static void Release(PlayerMobile player)
    {
        if (!BotSystem.IsPossessed(player))
        {
            return;
        }

        BotSystem.Unregister(player);
        player.SendMessage(0x59, "ИИ-контроль выключен — снова управляешь сам.");
    }
}

public class BotGoalGump : DynamicGump
{
    private readonly PlayerMobile _player;

    public BotGoalGump(PlayerMobile player) : base(50, 50) => _player = player;

    public override bool Singleton => true;

    private static readonly (string Label, BotGoal Goal)[] _choices =
    [
        ("Копать руду", BotGoals.Mine),
        ("Рубить дерево", BotGoals.Lumber),
        ("Рыбачить", BotGoals.Fish),
        ("Охотиться", BotGoals.Hunt),
        ("Ковать", BotGoals.Smithing),
        ("Решать самому", null)
    ];

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        var possessed = BotSystem.IsPossessed(_player);
        var height = 110 + (_choices.Length + (possessed ? 1 : 0)) * 25;

        builder.AddPage();
        builder.AddBackground(0, 0, 320, height, 5054);
        builder.AddAlphaRegion(10, 10, 300, height - 20);
        builder.AddHtml(15, 15, 290, 20, "Чем сейчас заняться?");

        var y = 45;
        for (var i = 0; i < _choices.Length; i++)
        {
            builder.AddButton(15, y, 4005, 4007, i + 1);
            builder.AddHtml(45, y, 260, 20, _choices[i].Label);
            y += 25;
        }

        if (possessed)
        {
            builder.AddButton(15, y, 4017, 4019, 100);
            builder.AddHtml(45, y, 260, 20, "Выключить ИИ-контроль");
            y += 25;
        }

        builder.AddHtml(15, y + 5, 290, 45, "Цель можно сменить в любой момент — снова [BecomeBot. Работает и после отключения от игры.");
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var id = info.ButtonID;

        if (id == 100)
        {
            BotPossession.Release(_player);
        }
        else if (id >= 1 && id <= _choices.Length)
        {
            BotPossession.Engage(_player, _choices[id - 1].Goal);
        }
    }
}
