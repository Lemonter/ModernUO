using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;

namespace Server.Systems.Bots;

/// <summary>
/// Перегруз, которому продажа не поможет: в сумке тяжело, а товара почти нет. Перегруженный бот
/// не может ни охотиться, ни добывать — каждая такая цель отказывает сразу, и без выхода он ходил
/// бы по кругу. Бот выбрасывает хлам, как игрок, — на землю, где тот истлеет. Одержимый персонаж
/// ([BecomeBot) ничего хозяйского не выбрасывает: несёт лишнее в банк.
/// </summary>
public sealed class UnloadGoal : BotGoal
{
    // Тяжелее этой доли от предела — перегруз; разгружается до второй.
    internal const double HeavyShare = 0.85;
    internal const double LightShare = 0.7;

    // Столько стоит товар, ради которого есть смысл сначала сходить на рынок.
    private const long WorthSelling = 300;

    public override string Name => "Разгрузка";

    public override bool IsUpkeep => true;

    internal static double Load(Mobile bot) => (double)(Mobile.BodyWeight + bot.TotalWeight) / System.Math.Max(1, bot.MaxWeight);

    public override double Score(BotBrain brain)
    {
        var bot = brain.Bot;
        if (Load(bot) < HeavyShare || BotGoods.ValueCarried(bot) >= WorthSelling || !HasJunk(brain))
        {
            return 0;
        }

        return 0.85;
    }

    public override List<BotAction> Plan(BotBrain brain)
    {
        if (!brain.IsPossessed)
        {
            return [new DropJunkAction()];
        }

        if (BotSocialRules.TownFor(brain.Bot) is not { } city || WorldCatalog.GetBanker(city) is not { } banker)
        {
            return null;
        }

        return [new GoToAction(banker, 3, "в банк, разгрузиться"), new DropJunkAction(banker)];
    }

    private static bool HasJunk(BotBrain brain)
    {
        foreach (var _ in Junk(brain))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Что можно отдать без ущерба для дела, тяжёлое первым: не товар, не деньги, не инструмент,
    /// не расходники боя и ремесла, не то, что бот держит для своего ремесла или себя.
    /// </summary>
    internal static IEnumerable<Item> Junk(BotBrain brain)
    {
        var bot = brain.Bot;
        if (bot.Backpack is not { } pack)
        {
            yield break;
        }

        var junk = new List<Item>();
        foreach (var item in pack.Items)
        {
            if (IsJunk(bot, item))
            {
                junk.Add(item);
            }
        }

        junk.Sort((a, b) => (b.TotalWeight + b.PileWeight).CompareTo(a.TotalWeight + a.PileWeight));

        foreach (var item in junk)
        {
            yield return item;
        }
    }

    private static bool IsJunk(PlayerMobile bot, Item item) =>
        item.Movable && item.LootType != LootType.Blessed && item.LootType != LootType.Newbied &&
        item is not (Gold or BankCheck or Container or BaseTool or BaseHarvestTool or Bandage or BaseReagent or BasePotion or
            Arrow or Bolt or Food or Spellbook or Runebook or RecallRune or Key or TreasureMap or Bottle or BlankScroll or
            SpellScroll or MahaonAnimalHead) &&
        !BotGoods.IsForSale(bot, item) &&
        !BotCrafting.KeepsForCraft(bot, item) && !BotCrafting.KeepsForOwnUse(bot, item) &&
        !BotQuests.WantsTrophy(bot, item) &&
        !(item is BaseWeapon weapon && IsSpareWeapon(bot, weapon));

    // Запасное оружие под свой лучший боевой навык не выбрасывают: им бот вооружится, потеряв своё.
    private static bool IsSpareWeapon(PlayerMobile bot, BaseWeapon weapon) =>
        bot.Skills[weapon.Skill].Value >= 50;
}

/// <summary>
/// Избавляется от хлама, тяжёлое первым, пока не полегчает: на землю под ноги, где вещь истлеет,
/// как у игрока, — или, у банкира, в банк (одержимый персонаж).
/// </summary>
public sealed class DropJunkAction : BotAction
{
    private const int BankerRange = 12;

    private readonly Banker _banker;

    public DropJunkAction(Banker banker = null) => _banker = banker;

    public override BotActionResult Tick(BotBrain brain)
    {
        var bot = brain.Bot;
        var bank = _banker == null ? null : bot.FindBankNoCreate() ?? bot.BankBox;

        if (_banker != null && (_banker.Deleted || !bot.InRange(_banker, BankerRange) || bank == null))
        {
            return BotActionResult.Failed();
        }

        var moved = 0;
        foreach (var item in new List<Item>(UnloadGoal.Junk(brain)))
        {
            if (UnloadGoal.Load(bot) < UnloadGoal.LightShare)
            {
                break;
            }

            if (bank != null)
            {
                if (bank.Items.Count >= bank.MaxItems || !bank.TryDropItem(bot, item, false))
                {
                    continue;
                }

                // Сданное в банк — хозяйское: BankGoal не понесёт его обратно на продажу.
                brain.GiveToOwner(item);
            }
            else
            {
                item.MoveToWorld(bot.Location, bot.Map);
            }

            brain.ForgetLoot(item);
            moved++;
        }

        return moved > 0 ? BotActionResult.Done(1000) : BotActionResult.Failed();
    }

    public override string Describe(BotBrain brain) => _banker == null ? "Выбрасывает хлам" : "Сдаёт лишнее в банк";
}
