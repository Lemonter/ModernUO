using System;
using System.Collections.Generic;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;

namespace Server.Systems.MahaonGuard;

/// <summary>
///     Rewards shop reachable from Сержант Гвидо's own gump — spends MahaonGuard points
///     (earned from raids/his own quest, see GuardSystem/GuardQuestSystem) on PowerScroll/
///     StatCapScroll/SkillMasteryPrimer. Replaces the old behavior where
///     SkillMasteryPrimer (and, separately, SakuroBlueprint) dropped as random loot from any
///     kill — the shard owner wanted that "mystery junk in the pack" gone and these bought
///     deliberately instead. Reuses the existing generic RewardGump/IRewardEntry UI (see
///     Gumps/RewardGump.cs) rather than building a new shop gump from scratch.
///
///     Catalog entries carry a Factory delegate instead of a live Item — the actual reward
///     Item is only ever constructed once, at purchase time inside OnPicked, after points
///     are successfully spent. Building one eagerly per catalog entry for display purposes
///     would both leak an unused Item into the world every time a player opens the shop AND
///     (for the randomized PowerScroll/primer entries) show a different roll than what
///     actually gets handed over.
/// </summary>
public enum GuardShopKind
{
    Other,
    PowerScroll,
    StatScroll,
    MasteryScroll
}

public static class GuardRewardShop
{
    private const int ScrollItemId = 0x14F0; // SpecialScroll's own base graphic
    private const int ScrollHue = 0x481; // matches PowerScroll/StatCapScroll's own Hue
    private const int PrimerItemId = 7714; // matches SkillMasteryPrimer's own base graphic
    private const int MasteryScrollHue = 0x48F; // отличает свитки мастерства от обычных

    // Подписи намеренно в две короткие строки с явным <br>: что это и до какого значения.
    // Поле описания в RewardGump — 145 пикселей, и длинная фраза в нём молча обрезается,
    // сколько ни подбирай высоту. Две строки по два десятка символов помещаются наверняка,
    // а подробности игрок видит в окне подтверждения, где места вдоволь.
    private sealed class Entry : IRewardEntry
    {
        public int Price { get; init; }
        public int ItemID { get; init; }
        public int Hue { get; init; }
        public int Tooltip { get; init; }
        public TextDefinition Description { get; init; }
        // Фабрика принимает покупателя: свиток силы подбирается под его навыки, а не
        // вслепую — см. MahaonScrollPicker.
        public Func<PlayerMobile, Item> Factory { get; init; }

        // What the entry sells and how much of it, for buyers that don't read the gump.
        public GuardShopKind Kind { get; init; }
        public int Value { get; init; }
    }

    // Свитки характеристик — наши (MahaonStatScroll), а не ванильные StatCapScroll.
    // Ванильный задаёт предел суммы абсолютным числом и отказывается работать, если у
    // игрока уже больше; а профессия у нас сама выставляет предел от 240 до 320, поэтому
    // ВСЕ ванильные свитки из этой лавки были заведомо бесполезны. Наш прибавляет.
    //
    // ВНИМАНИЕ к аргументам PowerScroll.CreateRandom: это НЕ значение свитка, а прибавка
    // сверх сотни. Внутри стоит «min /= 5; ... 100 + RandomMinMax(min, max) * 5», поэтому
    // CreateRandom(105, 105) даёт 100 + 21*5 = 205, а не 105 — ровно это и выдавала лавка.
    // Для свитка на 105 нужно передать 5, на 110 — 10, и так далее.
    //
    // У StatCapScroll и MahaonMasteryScroll аргумент, наоборот, абсолютный: 230 значит
    // потолок 230, 105.0 — потолок мастерки 105. Одинаковыми они не выглядят намеренно —
    // это разные классы с разной историей, а не наша непоследовательность.
    private static readonly IRewardEntry[] Catalog =
    {
        new Entry
        {
            Price = 5000, ItemID = ScrollItemId, Hue = ScrollHue,
            Description = "Свиток силы<br>потолок навыка → 105",
            Kind = GuardShopKind.PowerScroll, Value = 105,
            Factory = buyer => Systems.MahaonScrolls.MahaonScrollPicker.CreatePowerScrollFor(buyer, 105)
        },
        new Entry
        {
            Price = 12000, ItemID = ScrollItemId, Hue = ScrollHue,
            Description = "Свиток силы<br>потолок навыка → 110",
            Kind = GuardShopKind.PowerScroll, Value = 110,
            Factory = buyer => Systems.MahaonScrolls.MahaonScrollPicker.CreatePowerScrollFor(buyer, 110)
        },
        new Entry
        {
            Price = 25000, ItemID = ScrollItemId, Hue = ScrollHue,
            Description = "Свиток силы<br>потолок навыка → 115",
            Kind = GuardShopKind.PowerScroll, Value = 115,
            Factory = buyer => Systems.MahaonScrolls.MahaonScrollPicker.CreatePowerScrollFor(buyer, 115)
        },
        new Entry
        {
            Price = 50000, ItemID = ScrollItemId, Hue = ScrollHue,
            Description = "Свиток силы<br>потолок навыка → 120",
            Kind = GuardShopKind.PowerScroll, Value = 120,
            Factory = buyer => Systems.MahaonScrolls.MahaonScrollPicker.CreatePowerScrollFor(buyer, 120)
        },
        new Entry
        {
            Price = 4000, ItemID = ScrollItemId, Hue = ScrollHue,
            Description = "Свиток статов<br>сумма статов +5",
            Kind = GuardShopKind.StatScroll, Value = 5,
            Factory = _ => new MahaonStatScroll(5)
        },
        new Entry
        {
            Price = 9000, ItemID = ScrollItemId, Hue = ScrollHue,
            Description = "Свиток статов<br>сумма статов +10",
            Kind = GuardShopKind.StatScroll, Value = 10,
            Factory = _ => new MahaonStatScroll(10)
        },
        new Entry
        {
            Price = 16000, ItemID = ScrollItemId, Hue = ScrollHue,
            Description = "Свиток статов<br>сумма статов +15",
            Kind = GuardShopKind.StatScroll, Value = 15,
            Factory = _ => new MahaonStatScroll(15)
        },
        new Entry
        {
            Price = 25000, ItemID = ScrollItemId, Hue = ScrollHue,
            Description = "Свиток статов<br>сумма статов +20",
            Kind = GuardShopKind.StatScroll, Value = 20,
            Factory = _ => new MahaonStatScroll(20)
        },
        new Entry
        {
            Price = 40000, ItemID = ScrollItemId, Hue = ScrollHue,
            Description = "Свиток статов<br>сумма статов +25",
            Kind = GuardShopKind.StatScroll, Value = 25,
            Factory = _ => new MahaonStatScroll(25)
        },
        // Свитки силы на мастерки — вдвое дороже обычных свитков силы того же значения
        // (5000 -> 10000 и так далее). Поднимают потолок мастерки, а не навыка, см.
        // MahaonMasteryScroll / MahaonMasteryCapSystem.
        new Entry
        {
            Price = 10000, ItemID = ScrollItemId, Hue = MasteryScrollHue,
            Description = "Свиток мастерства<br>потолок мастерки → 105",
            Kind = GuardShopKind.MasteryScroll, Value = 105,
            Factory = _ => new MahaonMasteryScroll(105.0)
        },
        new Entry
        {
            Price = 24000, ItemID = ScrollItemId, Hue = MasteryScrollHue,
            Description = "Свиток мастерства<br>потолок мастерки → 110",
            Kind = GuardShopKind.MasteryScroll, Value = 110,
            Factory = _ => new MahaonMasteryScroll(110.0)
        },
        new Entry
        {
            Price = 50000, ItemID = ScrollItemId, Hue = MasteryScrollHue,
            Description = "Свиток мастерства<br>потолок мастерки → 115",
            Kind = GuardShopKind.MasteryScroll, Value = 115,
            Factory = _ => new MahaonMasteryScroll(115.0)
        },
        new Entry
        {
            Price = 100000, ItemID = ScrollItemId, Hue = MasteryScrollHue,
            Description = "Свиток мастерства<br>потолок мастерки → 120",
            Kind = GuardShopKind.MasteryScroll, Value = 120,
            Factory = _ => new MahaonMasteryScroll(120.0)
        },
        new Entry
        {
            Price = 3000, ItemID = PrimerItemId,
            Description = "Том мастерства I<br>случайная мастерка",
            Factory = buyer => RandomPrimer(buyer, 1)
        },
        new Entry
        {
            Price = 7000, ItemID = PrimerItemId,
            Description = "Том мастерства II<br>случайная мастерка",
            Factory = buyer => RandomPrimer(buyer, 2)
        },
        new Entry
        {
            Price = 15000, ItemID = PrimerItemId,
            Description = "Том мастерства III<br>случайная мастерка",
            Factory = buyer => RandomPrimer(buyer, 3)
        }
    };

    // Mahaon: платят золотом, а не очками стражи — очки остались только как валюта
    // званий/HP-бонуса в GuardSystem. Цены подняты вдесятеро относительно прежних очковых.
    public static void Open(PlayerMobile player) =>
        RewardGump.DisplayTo(
            player,
            "Награды стражи",
            DiscountedCatalog(player),
            SpendableGold(player),
            OnPicked,
            SubtitleFor(player)
        );

    /// <summary>Строка под заголовком: звание и что оно даёт в лавке.</summary>
    private static string SubtitleFor(PlayerMobile player)
    {
        var discount = (int)System.Math.Round((1.0 - GuardSystem.GetShopPriceScalar(player)) * 100);

        return discount > 0
            ? $"Звание «{GuardSystem.GetRankName(player)}» — скидка {discount}%"
            : $"Звание «{GuardSystem.GetRankName(player)}»";
    }

    /// <summary>Цена с учётом звания — см. GuardSystem.GetShopPriceScalar.</summary>
    private static int PriceFor(PlayerMobile player, Entry entry) =>
        System.Math.Max(1, (int)(entry.Price * GuardSystem.GetShopPriceScalar(player)));

    /// <summary>
    ///     Копия каталога с ценами этого игрока. Порядок тот же, что в Catalog, поэтому
    ///     индекс, который вернёт гамп, по-прежнему указывает на ту же награду.
    /// </summary>
    private static IRewardEntry[] DiscountedCatalog(PlayerMobile player)
    {
        var result = new IRewardEntry[Catalog.Length];

        for (var i = 0; i < Catalog.Length; i++)
        {
            var entry = (Entry)Catalog[i];

            result[i] = new Entry
            {
                Price = PriceFor(player, entry),
                ItemID = entry.ItemID,
                Hue = entry.Hue,
                Tooltip = entry.Tooltip,
                Description = entry.Description,
                Factory = entry.Factory,
                Kind = entry.Kind,
                Value = entry.Value
            };
        }

        return result;
    }

    /// <summary>
    ///     Сколько золота игрок может здесь потратить — из рюкзака И из банка.
    ///
    ///     Banker.GetBalance смотрит ТОЛЬКО в банк (плюс чеки и золото аккаунта), а золото
    ///     после охоты лежит в рюкзаке. Магазин показывал ноль и гасил все строки: звание
    ///     есть, деньги есть, а купить нельзя — потому что магазин их не видел.
    /// </summary>
    public static int SpendableGold(Mobile player)
    {
        var total = (long)Banker.GetBalance(player);

        if (player.Backpack != null)
        {
            foreach (var gold in player.Backpack.FindItemsByType<Gold>())
            {
                total += gold.Amount;
            }
        }


        return (int)System.Math.Clamp(total, 0, int.MaxValue);
    }

    /// <summary>Берёт цену сначала из рюкзака, остаток — из банка.</summary>
    private static bool TakeGold(Mobile player, int amount)
    {
        if (SpendableGold(player) < amount)
        {
            return false;
        }

        if (player.Backpack != null)
        {
            // Сначала собираем список, потом тратим. Delete() внутри перечисления рушит
            // сам перечислитель — «Item was modified after enumerator was instantiated»
            // прилетал прямо в обработчик кнопки, и покупка обрывалась на середине.
            var purse = new List<Gold>();

            foreach (var gold in player.Backpack.FindItemsByType<Gold>())
            {
                purse.Add(gold);
            }

            foreach (var gold in purse)
            {
                if (amount <= 0)
                {
                    break;
                }

                if (gold.Deleted)
                {
                    continue;
                }

                if (gold.Amount <= amount)
                {
                    amount -= gold.Amount;
                    gold.Delete();
                }
                else
                {
                    gold.Amount -= amount;
                    amount = 0;
                }
            }
        }

        return amount <= 0 || Banker.Withdraw(player, amount);
    }

    // Mahaon: NOT SkillMasteryPrimer.GetRandom() — that constructs a full throwaway Item
    // just to read its randomly-picked Skill, which would leak an orphaned, never-deleted
    // primer into the world on every purchase. Same random-skill-pick logic
    // (MasteryInfo.Skills is the public array GetRandom() itself reads from), just without
    // instantiating an Item to get there.
    /// <summary>Порог, ниже которого навык не считается «своим» — том мастерства по нему
    /// покупателю ни к чему.</summary>
    private const double PrimerSkillFloor = 30.0;

    private static SkillMasteryPrimer RandomPrimer(PlayerMobile buyer, int volume)
    {
        var all = Spells.SkillMasteries.MasteryInfo.Skills;
        var candidates = new List<SkillName>();

        // Том по навыку, которого у покупателя нет, — такая же бумага, как свиток силы на
        // выкачанный навык. Берём только те, которыми он действительно занимается и по
        // которым этот том ещё не изучен.
        foreach (var skill in all)
        {
            if (buyer?.Skills?[skill]?.Value >= PrimerSkillFloor &&
                !Spells.SkillMasteries.MasteryInfo.HasLearned(buyer, skill, volume))
            {
                candidates.Add(skill);
            }
        }

        var chosen = candidates.Count > 0
            ? candidates[Utility.Random(candidates.Count)]
            : all[Utility.Random(all.Length)];

        return new SkillMasteryPrimer(chosen, volume);
    }

    /// <summary>The catalog line selling this kind of reward at this value, or -1.</summary>
    public static int IndexOf(GuardShopKind kind, int value)
    {
        for (var i = 0; i < Catalog.Length; i++)
        {
            if (Catalog[i] is Entry entry && entry.Kind == kind && entry.Value == value)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>What this buyer pays for a catalog line, its rank's discount taken off.</summary>
    public static int PriceOf(PlayerMobile player, int index) =>
        index >= 0 && index < Catalog.Length ? PriceFor(player, (Entry)Catalog[index]) : int.MaxValue;

    /// <summary>Buys a catalog line, as the line's button in the shop gump does.</summary>
    public static void Buy(PlayerMobile player, int index) => OnPicked(player, index);

    private static void OnPicked(Mobile from, int index)
    {
        if (from is not PlayerMobile player || index < 0 || index >= Catalog.Length)
        {
            return;
        }

        var entry = (Entry)Catalog[index];
        var price = PriceFor(player, entry);

        if (!TakeGold(player, price))
        {
            player.SendMessage(0x22, $"Не хватает золота — нужно {price}.");
            return;
        }

        var reward = entry.Factory(player);

        if (player.Backpack?.TryDropItem(player, reward, false) != true)
        {
            reward.MoveToWorld(player.Location, player.Map);
        }

        var saved = entry.Price - price;

        player.SendMessage(
            0x59,
            saved > 0
                ? $"Куплено за {price} золота ({saved} скинуто за звание «{GuardSystem.GetRankName(player)}»)."
                : $"Куплено за {price} золота."
        );
    }
}
