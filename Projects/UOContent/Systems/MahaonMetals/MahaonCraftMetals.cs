using System.Collections.Generic;
using Server.Engines.Craft;
using Server.Items;

namespace Server.Systems.MahaonMetals;

/// <summary>
///     Мост между нашими металлами и ванильным движком крафта.
///
///     Задача. Движок опознаёт ресурсы ПО ТИПУ C#: подресурсы объявлены как
///     typeof(IronIngot), typeof(DullCopperIngot) и так далее, счётчик в гампе считает
///     через IsInstanceOfType, а ConsumeRes собирает Type[] и зовёт GetAmount/ConsumeTotal.
///     Наш металл устроен наоборот: MahaonIngot — ОДИН класс на все 24 металла, сам металл
///     лежит полем. Из-за этого добытый металл в меню крафта не появлялся вовсе, а
///     магазинное железо появлялось: оно ванильного типа.
///
///     Решение. Все 24 подресурса объявляются одним и тем же типом MahaonIngot, а какой
///     именно металл выбран — берётся из НОМЕРА выбранной строки, а не из типа. Номер и
///     так живёт в CraftContext.LastResourceIndex: именно им гамп подсвечивает выбор, так
///     что ничего нового заводить не пришлось.
///
///     Почему не 24 подкласса (IronIngot, CobaltIngot, ...). Во-первых, MahaonIngot уже
///     лежит в сохранениях мира, и смена типа сломала бы десериализацию. Во-вторых,
///     двадцать четыре пустых класса ради одного поля — ровно та дупликация, которая уже
///     однажды вышла боком: проверку «куда можно вставить камень» продублировали в двух
///     местах, поправили в одном, и робы перестали инкрустироваться.
/// </summary>
public static class MahaonCraftMetals
{
    /// <summary>
    ///     Объявляет все 24 металла подресурсами системы крафта.
    ///
    ///     Тип у всех записей один — MahaonIngot. Движку этого достаточно: мутация ресурса
    ///     в ConsumeRes срабатывает по совпадению ИСХОДНОГО типа рецепта с ResType
    ///     коллекции (typeof(IronIngot)), а подставляет уже тип выбранной строки. Поэтому
    ///     сами рецепты переписывать не нужно — они как ссылались на typeof(IronIngot),
    ///     так и ссылаются, это теперь просто метка «здесь идёт металл».
    /// </summary>
    public static void AddMetalSubResources(CraftSystem system)
    {
        foreach (var metal in MetalOrder)
        {
            var info = MahaonMetalTable.Get(metal);

            // Требование по навыку берётся из требования к ДОБЫЧЕ этого металла. Своей
            // шкалы «сложность ковки» у нас нет, а редкость — единственное осмысленное
            // число, которое уже есть на каждый металл: что тяжелее добыть, тем тяжелее и
            // отковать. Если понадобится развести добычу и ковку — здесь и разводить.
            system.AddSubRes(
                typeof(MahaonIngot),
                info.RuName,
                info.MiningSkillRequired,
                1044036, // You have worked the metal into an item.
                $"Нужно больше мастерства, чтобы ковать «{info.RuName}».",
                metal
            );
        }
    }

    /// <summary>Порядок строк в меню — он же порядок редкости, он же порядок enum.</summary>
    public static readonly MahaonMetal[] MetalOrder =
    {
        MahaonMetal.Iron, MahaonMetal.Cobalt, MahaonMetal.Shadow, MahaonMetal.Cooper,
        MahaonMetal.Bronze, MahaonMetal.Silver, MahaonMetal.Gold, MahaonMetal.Melchior,
        MahaonMetal.Ice, MahaonMetal.Crystal, MahaonMetal.Agapite, MahaonMetal.Winter,
        MahaonMetal.Verite, MahaonMetal.Onyx, MahaonMetal.Sky,
        MahaonMetal.Doom, MahaonMetal.Diamond, MahaonMetal.Valorite, MahaonMetal.Titanium,
        MahaonMetal.Mytheril, MahaonMetal.Mahaon, MahaonMetal.Kotium, MahaonMetal.Pawerium,
        MahaonMetal.Lemium
    };

    /// <summary>
    ///     Какой металл выбран игроком прямо сейчас.
    ///
    ///     Читается из контекста, а не протаскивается параметром. Протащить пришлось бы
    ///     через полтора десятка сигнатур (Craft, CompleteCraft, ConsumeRes, CheckSkills,
    ///     InternalTimer, гампы клейма и фракций), причём каждая из них к металлу
    ///     отношения не имеет. Контекст же и так доступен везде, где есть craftSystem и
    ///     from, и именно в нём движок уже хранит выбранную строку.
    ///
    ///     Возвращает null, когда металл ни при чём: система не металлическая, строка не
    ///     выбрана, или выбранная строка — не наша.
    /// </summary>
    public static MahaonMetal? SelectedMetal(CraftSystem system, Mobile from)
    {
        var context = system?.GetContext(from);

        if (context == null)
        {
            return null;
        }

        var index = context.LastResourceIndex;
        var col = system.CraftSubRes;

        if (index < 0 || index >= col.Count)
        {
            // Строку ещё не трогали — движок в этом случае берёт первую.
            return col.Count > 0 ? col.GetAt(0).Metal : null;
        }

        return col.GetAt(index).Metal;
    }

    /// <summary>Сколько слитков ИМЕННО ЭТОГО металла лежит в рюкзаке.</summary>
    public static int CountIngots(Container pack, MahaonMetal metal)
    {
        if (pack == null)
        {
            return 0;
        }

        var total = 0;

        foreach (var item in pack.FindItemsByType<MahaonIngot>())
        {
            if (item.Metal == metal)
            {
                total += item.Amount;
            }
        }

        return total;
    }

    /// <summary>
    ///     Тратит нужное количество слитков этого металла. Возвращает false и не трогает
    ///     ничего, если столько не набралось — проверка идёт до списания, иначе при
    ///     нехватке половина стопок уже исчезла бы.
    /// </summary>
    public static bool ConsumeIngots(Container pack, MahaonMetal metal, int amount)
    {
        if (pack == null || amount <= 0)
        {
            return amount <= 0;
        }

        var stacks = new List<MahaonIngot>();
        var available = 0;

        foreach (var item in pack.FindItemsByType<MahaonIngot>())
        {
            if (item.Metal != metal)
            {
                continue;
            }

            stacks.Add(item);
            available += item.Amount;
        }

        if (available < amount)
        {
            return false;
        }

        var left = amount;

        foreach (var stack in stacks)
        {
            if (left <= 0)
            {
                break;
            }

            var take = left < stack.Amount ? left : stack.Amount;
            stack.Consume(take);
            left -= take;
        }

        return true;
    }

    /// <summary>
    ///     Тратит СКОЛЬКО ПОЛУЧИТСЯ, но не больше указанного, и возвращает потраченное.
    ///     В отличие от ConsumeIngots частичный результат здесь — нормальный исход: так
    ///     работает починка голема, где каждый слиток чинит свою долю урона.
    /// </summary>
    public static int ConsumeUpToIngots(Container pack, MahaonMetal metal, int max)
    {
        if (pack == null || max <= 0)
        {
            return 0;
        }

        var taken = 0;

        foreach (var stack in pack.FindItemsByType<MahaonIngot>())
        {
            if (taken >= max)
            {
                break;
            }

            if (stack.Metal != metal)
            {
                continue;
            }

            var left = max - taken;
            var take = left < stack.Amount ? left : stack.Amount;

            stack.Consume(take);
            taken += take;
        }

        return taken;
    }

    /// <summary>
    ///     Клеймит свежесделанную вещь металлом: запись в трекер, цвет, обновление
    ///     свойств. Ровно то же, что делала перековка молотом, только сразу и без второго
    ///     шага.
    /// </summary>
    public static void StampCraftedItem(Item item, MahaonMetal metal)
    {
        if (item == null)
        {
            return;
        }

        MahaonMetalTracker.SetMetal(item, metal);

        var info = MahaonMetalTable.Get(metal);

        // Железо не красим: у него хью 0, и присваивание затёрло бы цвет, который вещь
        // могла получить от рецепта.
        if (info.Hue != 0)
        {
            item.Hue = info.Hue;
        }

        item.InvalidateProperties();
    }
}
