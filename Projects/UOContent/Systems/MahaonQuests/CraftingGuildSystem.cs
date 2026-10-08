using System;
using Server.Items;
using Server.Systems.MahaonMetals;

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

    // Counts narrows the type further: the shard's ingots are one class with the metal as a field.
    private readonly record struct OrderKind(string RuName, Func<Item> Factory, Type ItemType, Func<Item, bool> Counts = null);

    private static readonly OrderKind[] OrderPool =
    {
        new(
            "железные слитки",
            () => new MahaonIngot(),
            typeof(MahaonIngot),
            item => item is MahaonIngot { Metal: MahaonMetal.Iron }
        ),
        new("доски", () => new Board(), typeof(Board)),
        new("выделанную кожу", () => new Leather(), typeof(Leather)),
        new("бинты", () => new Bandage(), typeof(Bandage)),
        new("стрелы", () => new Arrow(), typeof(Arrow))
    };

    private static OrderKind _currentKind;
    private static int _currentAmount;

    /// <summary>Changes with every new order, so whoever read the board knows when it is stale.</summary>
    public static int OrderStamp { get; private set; }

    public static Type CurrentItemType => _currentKind.ItemType;

    public static int CurrentAmount => _currentAmount;

    public static int RewardPerUnit => GoldPerUnit;

    public static void Initialize()
    {
        RollOrder();
        Timer.DelayCall(RerollInterval, RerollInterval, RollOrder);
    }

    internal static void RollOrder()
    {
        _currentKind = OrderPool[Utility.Random(OrderPool.Length)];
        _currentAmount = Utility.RandomMinMax(20, 60);
        OrderStamp++;
    }

    public static string CurrentOrderText() =>
        $"Гильдии ремесленников нужны {_currentKind.RuName}: {_currentAmount} шт. Плата: {GoldPerUnit} золота за штуку.";

    /// <summary>How much of what the current order wants this container holds.</summary>
    public static int CountIn(Container pack)
    {
        if (_currentKind.ItemType == null)
        {
            return 0;
        }

        var total = 0;
        foreach (var item in pack.FindItemsByType(_currentKind.ItemType))
        {
            if (_currentKind.Counts?.Invoke(item) != false)
            {
                total += item.Amount;
            }
        }

        return total;
    }

    private static void Take(Container pack, int amount)
    {
        foreach (var item in pack.EnumerateItemsByType<Item>())
        {
            if (amount <= 0)
            {
                break;
            }

            if (_currentKind.ItemType.IsInstanceOfType(item) && _currentKind.Counts?.Invoke(item) != false)
            {
                var take = Math.Min(amount, item.Amount);
                item.Consume(take);
                amount -= take;
            }
        }
    }

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

        var have = CountIn(backpack);

        if (have < _currentAmount)
        {
            from.SendMessage(
                0x22,
                $"Не хватает — нужно {_currentAmount} ({_currentKind.RuName}), у тебя {have}. {CurrentOrderText()}"
            );
            return;
        }

        Take(backpack, _currentAmount);

        var reward = _currentAmount * GoldPerUnit;
        backpack.DropItem(new Gold(reward));

        from.SendMessage(0x59, $"Заказ выполнен! Получено {reward} золота.");

        RollOrder();
    }
}
