using System;
using Server.Items;

namespace Server.Systems.MahaonQuests;

/// <summary>
///     Third quest-giver on the same QuestMarkerRegistry foundation — a crafting guild
///     order board wants a stack of some basic material/craftable, pays gold on the spot
///     when a player double-clicks the board while carrying enough of it. No separate
///     "accept" step, no target-cursor flow — simplest possible turn-in: have it, click
///     the board, it's gone and you're paid, same low-friction shape as the bounty board.
/// </summary>
public static class CraftingGuildSystem
{
    private static readonly TimeSpan RerollInterval = TimeSpan.FromMinutes(30);
    private const int GoldPerUnit = 8;

    private readonly record struct OrderKind(string RuName, Func<Item> Factory, Type ItemType);

    private static readonly OrderKind[] OrderPool =
    {
        new("железные слитки", () => new IronIngot(), typeof(IronIngot)),
        new("доски", () => new Board(), typeof(Board)),
        new("выделанную кожу", () => new Leather(), typeof(Leather)),
        new("бинты", () => new Bandage(), typeof(Bandage)),
        new("стрелы", () => new Arrow(), typeof(Arrow))
    };

    private static OrderKind _currentKind;
    private static int _currentAmount;

    public static void Initialize()
    {
        RollOrder();
        Timer.DelayCall(RerollInterval, RerollInterval, RollOrder);
    }

    private static void RollOrder()
    {
        _currentKind = OrderPool[Utility.Random(OrderPool.Length)];
        _currentAmount = Utility.RandomMinMax(20, 60);
    }

    public static string CurrentOrderText() =>
        $"Гильдии ремесленников нужны {_currentKind.RuName}: {_currentAmount} шт. Плата: {GoldPerUnit} золота за штуку.";

    /// <summary>Called from MahaonCraftingOrderBoard.OnDoubleClick — tries to consume
    /// enough of the current material from the player's backpack, pays if successful.</summary>
    public static void TryFulfill(Mobile from)
    {
        var backpack = from.Backpack;

        if (backpack == null)
        {
            from.SendMessage(0x22, "У тебя нет с собой рюкзака.");
            return;
        }

        var have = backpack.GetAmount(_currentKind.ItemType);

        if (have < _currentAmount)
        {
            from.SendMessage(
                0x22,
                $"Не хватает — нужно {_currentAmount} ({_currentKind.RuName}), у тебя {have}. {CurrentOrderText()}"
            );
            return;
        }

        backpack.ConsumeTotal(_currentKind.ItemType, _currentAmount);

        var reward = _currentAmount * GoldPerUnit;
        backpack.DropItem(new Gold(reward));

        from.SendMessage(0x59, $"Заказ выполнен! Получено {reward} золота.");

        RollOrder();
    }
}
