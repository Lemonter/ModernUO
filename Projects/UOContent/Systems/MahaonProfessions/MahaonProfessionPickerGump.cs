using System.Collections.Generic;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Systems.MahaonProfessions;

public enum ProfessionGumpStage
{
    CategorySelect,
    ProfessionSelect,
    Confirm,

    /// <summary>Выбор третьего пути — категории поверх двух, заданных профессией.</summary>
    ThirdSelect
}

/// <summary>
///     Referenced by ProfessionStone.cs — category select, then profession select within
///     that category, then a confirmation screen showing everything the profession
///     actually does (primary/secondary category, stat caps, full skill list with caps)
///     before committing, with Confirm/Back. Also offers a 1000-gold reset from the
///     category screen if the player already has a profession.
/// </summary>
public class MahaonProfessionPickerGump : StaticGump<MahaonProfessionPickerGump>
{
    private const int ResetCost = 1000; // gold

    private readonly PlayerMobile _player;
    private readonly ProfessionGumpStage _stage;
    private readonly ProfessionCategory _category;
    private readonly MahaonProfession? _pending;

    public override bool Singleton => true;
    protected override bool Cached => false;

    public MahaonProfessionPickerGump(
        PlayerMobile player,
        ProfessionGumpStage stage = ProfessionGumpStage.CategorySelect,
        ProfessionCategory category = ProfessionCategory.Magic,
        MahaonProfession? pending = null
    ) : base(80, 80)
    {
        _player = player;
        _stage = stage;
        _category = category;
        _pending = pending;
    }

    // Здесь категории названы по деятелю ("кем ты станешь"), а не по домену, как в
    // ProfessionData.RuCategoryName — гамп спрашивает "выбери путь", и "Некромант" тут
    // читается лучше, чем "Некромантия".
    private static string CategoryName(ProfessionCategory c) => c switch
    {
        ProfessionCategory.Magic      => "Маг",
        ProfessionCategory.Craft      => "Ремесленник",
        ProfessionCategory.Warrior    => "Воин",
        ProfessionCategory.Thief      => "Вор",
        ProfessionCategory.Bard       => "Бард",
        ProfessionCategory.Ranger     => "Следопыт",
        ProfessionCategory.Necromancy => "Некромант",
        ProfessionCategory.Faith      => "Служитель веры",
        ProfessionCategory.Mysticism  => "Мистик",
        ProfessionCategory.Weaving    => "Плетущий чары",
        ProfessionCategory.Bushido    => "Путь воина",
        ProfessionCategory.Ninjitsu   => "Ниндзя",
        _                             => c.ToString()
    };

    protected override void BuildLayout(ref StaticGumpBuilder builder)
    {
        switch (_stage)
        {
            case ProfessionGumpStage.ThirdSelect:
                BuildThirdSelect(ref builder);
                break;
            case ProfessionGumpStage.Confirm:
                BuildConfirm(ref builder);
                break;
            case ProfessionGumpStage.ProfessionSelect:
                BuildProfessionSelect(ref builder);
                break;
            default:
                BuildCategorySelect(ref builder);
                break;
        }
    }

    private void BuildCategorySelect(ref StaticGumpBuilder builder)
    {
        var current = ProfessionSystem.GetProfession(_player);

        // Высота считается от числа категорий, а не задана числом: категорий стало
        // двенадцать вместо шести, и прежние 300 пикселей обрезали половину списка.
        var categoryCount = System.Enum.GetValues<ProfessionCategory>().Length;
        var height = 65 + categoryCount * 28 + (current != null ? 84 : 0);

        builder.AddPage();
        builder.AddBackground(0, 0, 340, height, 5054);
        builder.AddAlphaRegion(10, 10, 320, height - 20);

        builder.AddHtml(20, 15, 280, 20, "Камень профессий — выбери путь");

        var y = 45;
        foreach (var category in System.Enum.GetValues<ProfessionCategory>())
        {
            builder.AddButton(20, y, 4005, 4007, 100 + (int)category);
            builder.AddLabel(55, y + 2, 0x480, CategoryName(category));

            // Одно слово о том, чем этот путь отличается от соседнего — иначе список из
            // двенадцати одинаковых кнопок ни о чём не говорит.
            var signature = ProfessionPerks.Signature(category);

            if (signature != null)
            {
                builder.AddLabel(150, y + 2, 0x3B2, signature.Value.Name);
            }

            y += 28;
        }

        if (current != null)
        {
            y += 10;
            builder.AddHtml(20, y, 300, 20, $"Сейчас: {ProfessionData.GetName(current.Value, _player.Female)}");
            y += 24;

            var third = ProfessionSystem.GetThirdCategory(_player);

            if (third == null)
            {
                builder.AddButton(20, y, 4005, 4007, 997);
                builder.AddLabel(55, y + 2, 0x59, "Выбрать третий путь");
            }
            else
            {
                builder.AddHtml(20, y, 300, 20, $"Третий путь: {CategoryName(third.Value)}");
            }

            y += 24;
            builder.AddButton(20, y, 4005, 4007, 999);
            builder.AddLabel(55, y + 2, 0x22, $"Сбросить профессию ({ResetCost} золота)");
        }
    }

    /// <summary>
    ///     Третий путь. В отличие от первых двух его не задаёт профессия — игрок берёт его
    ///     сам, любой категорией, кроме двух своих. Даёт только сигнатурный перк выбранной
    ///     категории и потолок её мастерок 105: это приправа к образу, а не второй полный
    ///     набор, иначе профессия перестала бы быть узнаваемой.
    /// </summary>
    private void BuildThirdSelect(ref StaticGumpBuilder builder)
    {
        var current = ProfessionSystem.GetProfession(_player);

        if (current == null)
        {
            BuildCategorySelect(ref builder);
            return;
        }

        var info = ProfessionData.All[current.Value];

        var available = new List<ProfessionCategory>();
        foreach (var category in System.Enum.GetValues<ProfessionCategory>())
        {
            if (category != info.Category && category != info.SecondaryCategory)
            {
                available.Add(category);
            }
        }

        var height = 130 + available.Count * 46;

        builder.AddPage();
        builder.AddBackground(0, 0, 360, height, 5054);
        builder.AddAlphaRegion(10, 10, 340, height - 20);

        builder.AddHtml(20, 15, 320, 20, "<BASEFONT COLOR=#FFD700>Третий путь</BASEFONT>");
        builder.AddHtml(
            20, 38, 320, 40,
            "Одна категория поверх твоих двух. Даёт её главный перк и потолок её мастерок 105. Выбирается один раз — сменить можно только вместе с профессией."
        );

        var y = 85;
        foreach (var category in available)
        {
            builder.AddButton(20, y, 4005, 4007, 300 + (int)category);
            builder.AddLabel(55, y + 2, 0x480, CategoryName(category));

            var signature = ProfessionPerks.Signature(category);

            if (signature != null)
            {
                builder.AddHtml(55, y + 20, 290, 24, $"<BASEFONT COLOR=#9FD8FF>{signature.Value.Name}</BASEFONT> — {signature.Value.Description}");
            }

            y += 46;
        }

        y += 5;
        builder.AddButton(20, y, 4005, 4007, 998); // назад
        builder.AddLabel(55, y + 2, 0x480, "Позже");
    }

    private void BuildProfessionSelect(ref StaticGumpBuilder builder)
    {
        var professions = ProfessionData.InCategory(_category);
        var height = 90 + professions.Count * 28;

        builder.AddPage();
        builder.AddBackground(0, 0, 360, height, 5054);
        builder.AddAlphaRegion(10, 10, 340, height - 20);

        builder.AddHtml(20, 15, 280, 20, $"Путь: {CategoryName(_category)}");

        var y = 45;
        foreach (var profession in professions)
        {
            // Профессии внутри категории различаются как раз вторым путём — без него
            // список читается как пять одинаковых кнопок с разными словами.
            var secondary = ProfessionData.All[profession].SecondaryCategory;

            builder.AddButton(20, y, 4005, 4007, 200 + (int)profession);
            builder.AddLabel(55, y + 2, 0x480, ProfessionData.GetName(profession, _player.Female));
            builder.AddLabel(200, y + 2, 0x3B2, $"+ {CategoryName(secondary)}");
            y += 28;
        }

        y += 5;
        builder.AddButton(20, y, 4005, 4007, 998); // back to category select
        builder.AddLabel(55, y + 2, 0x480, "Назад");
    }

    private void BuildConfirm(ref StaticGumpBuilder builder)
    {
        if (_pending == null)
        {
            BuildCategorySelect(ref builder);
            return;
        }

        var profession = _pending.Value;
        var info = ProfessionData.All[profession];
        var primarySkills = ProfessionData.CategorySkills.GetValueOrDefault(info.Category, System.Array.Empty<SkillName>());

        var perks = ProfessionPerks.For(info.Category);
        var secondaryPerk = ProfessionPerks.Signature(info.SecondaryCategory);

        // Высота считается по содержимому: перки занимают по две строки каждый (название
        // и описание), у вторичной категории показывается один сигнатурный.
        var height = 285 + primarySkills.Length * 20 + perks.Count * 42 +
                     (secondaryPerk != null ? 42 : 0) +
                     (ProfessionSystem.GetThirdCategory(_player) != null ? 42 : 0);

        builder.AddPage();
        builder.AddBackground(0, 0, 340, height, 5054);
        builder.AddAlphaRegion(10, 10, 320, height - 20);

        builder.AddHtml(20, 15, 300, 20, $"<BASEFONT COLOR=#FFD700>{ProfessionData.GetName(profession, _player.Female)}</BASEFONT>");

        // Раньше здесь стояло «Основа: Маг — доп.: Ремесленник», и что означает «доп.»,
        // игроку взять было неоткуда. Теперь каждая строка сама говорит, что даёт.
        builder.AddHtml(
            20, 40, 300, 20,
            $"Основной путь: <BASEFONT COLOR=#FFD700>{CategoryName(info.Category)}</BASEFONT> — все три перка, навыки до 120"
        );

        builder.AddHtml(
            20, 60, 300, 20,
            $"Второй путь: <BASEFONT COLOR=#FFD700>{CategoryName(info.SecondaryCategory)}</BASEFONT> — только главный перк"
        );

        // Пределов характеристик по профессиям больше нет — они и не работали: для
        // игрока отдельную характеристику ограничивает общешардовая настройка
        // stats.statMax, а сумму — StatCap, и профессия влияла только на второе. Обещать
        // в описании то, чего система не делает, хуже, чем не обещать ничего.
        builder.AddHtml(20, 88, 300, 20, "Навыки основного пути (потолок 120, у остальных — 100):");

        var y = 111;
        foreach (var skill in primarySkills)
        {
            // Русские имена навыков — те же, что во всём остальном интерфейсе; раньше
            // здесь печаталось имя из перечисления, то есть английское.
            builder.AddHtml(35, y, 280, 20, $"— {MahaonCombat.MahaonSkillTree.RuSkillName(skill)}");
            y += 20;
        }

        y += 15;
        builder.AddHtml(
            20, y, 300, 40,
            "Все остальные навыки будут ограничены потолком 100, как у всех, без класса не выше этого."
        );
        y += 45;

        // Ради чего путь вообще выбирают. Без этого списка выбор профессии выглядел как
        // выбор потолков навыков и ничего больше.
        builder.AddHtml(20, y, 300, 20, "<BASEFONT COLOR=#FFD700>Что даёт этот путь:</BASEFONT>");
        y += 24;

        foreach (var perk in perks)
        {
            var mark = perk.Signature ? " (главный — работает и как второй путь)" : "";
            builder.AddHtml(30, y, 290, 20, $"<BASEFONT COLOR=#9FD8FF>{perk.Name}</BASEFONT>{mark}");
            y += 18;
            builder.AddHtml(30, y, 290, 24, perk.Description);
            y += 24;
        }

        var thirdCategory = ProfessionSystem.GetThirdCategory(_player);
        var thirdPerk = thirdCategory != null ? ProfessionPerks.Signature(thirdCategory.Value) : null;

        if (secondaryPerk != null)
        {
            y += 6;
            builder.AddHtml(
                20, y, 300, 20,
                $"<BASEFONT COLOR=#FFD700>От второго пути ({CategoryName(info.SecondaryCategory)}):</BASEFONT>"
            );
            y += 22;
            builder.AddHtml(30, y, 290, 20, $"<BASEFONT COLOR=#9FD8FF>{secondaryPerk.Value.Name}</BASEFONT>");
            y += 18;
            builder.AddHtml(30, y, 290, 24, secondaryPerk.Value.Description);
            y += 24;
        }

        if (thirdPerk != null)
        {
            y += 6;
            builder.AddHtml(
                20, y, 300, 20,
                $"<BASEFONT COLOR=#FFD700>От третьего пути ({CategoryName(thirdCategory.Value)}):</BASEFONT>"
            );
            y += 22;
            builder.AddHtml(30, y, 290, 20, $"<BASEFONT COLOR=#9FD8FF>{thirdPerk.Value.Name}</BASEFONT>");
            y += 18;
            builder.AddHtml(30, y, 290, 24, thirdPerk.Value.Description);
            y += 24;
        }

        y += 10;

        builder.AddButton(20, y, 4005, 4007, 998); // back to profession select
        builder.AddLabel(55, y + 2, 0x480, "Назад");

        builder.AddButton(150, y, 4023, 4025, 1); // confirm
        builder.AddLabel(185, y + 2, 0x59, "Подтвердить");
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var mobile = sender.Mobile;
        if (mobile is not PlayerMobile pm)
        {
            return;
        }

        var buttonId = info.ButtonID;

        switch (_stage)
        {
            case ProfessionGumpStage.CategorySelect:
                if (buttonId == 999) // Reset
                {
                    if (pm.Backpack == null || !pm.Backpack.ConsumeTotal(typeof(Gold), ResetCost))
                    {
                        pm.SendMessage(0x22, $"Недостаточно золота — нужно {ResetCost}.");
                        pm.SendGump(new MahaonProfessionPickerGump(pm));
                        return;
                    }

                    ProfessionSystem.ClearProfession(pm);
                    pm.SendMessage(0x59, "Профессия сброшена.");
                    return;
                }

                if (buttonId == 997) // выбор третьего пути
                {
                    pm.SendGump(new MahaonProfessionPickerGump(pm, ProfessionGumpStage.ThirdSelect));
                    return;
                }

                if (buttonId is >= 100 and < 200)
                {
                    var category = (ProfessionCategory)(buttonId - 100);
                    pm.SendGump(new MahaonProfessionPickerGump(pm, ProfessionGumpStage.ProfessionSelect, category));
                }

                break;

            case ProfessionGumpStage.ThirdSelect:
                if (buttonId == 998) // позже
                {
                    pm.SendGump(new MahaonProfessionPickerGump(pm));
                    return;
                }

                if (buttonId is >= 300 and < 400)
                {
                    var third = (ProfessionCategory)(buttonId - 300);

                    if (ProfessionSystem.SetThirdCategory(pm, third))
                    {
                        var signature = ProfessionPerks.Signature(third);
                        pm.SendMessage(
                            0x59,
                            signature != null
                                ? $"Третий путь: {CategoryName(third)}. Теперь тебе доступно «{signature.Value.Name}»."
                                : $"Третий путь: {CategoryName(third)}."
                        );
                    }
                    else
                    {
                        pm.SendMessage(0x22, "Этот путь тебе взять нельзя.");
                    }

                    pm.SendGump(new MahaonProfessionPickerGump(pm));
                }

                break;

            case ProfessionGumpStage.ProfessionSelect:
                if (buttonId == 998) // back
                {
                    pm.SendGump(new MahaonProfessionPickerGump(pm));
                    return;
                }

                if (buttonId is >= 200 and < 300)
                {
                    var profession = (MahaonProfession)(buttonId - 200);
                    pm.SendGump(new MahaonProfessionPickerGump(pm, ProfessionGumpStage.Confirm, _category, profession));
                }

                break;

            case ProfessionGumpStage.Confirm:
                if (buttonId == 998) // back
                {
                    pm.SendGump(new MahaonProfessionPickerGump(pm, ProfessionGumpStage.ProfessionSelect, _category));
                    return;
                }

                if (buttonId == 1 && _pending != null) // confirm
                {
                    ProfessionSystem.SetProfession(pm, _pending.Value);
                    pm.SendMessage(0x59, $"Ты теперь {ProfessionData.GetName(_pending.Value, pm.Female)}.");

                    // Сразу предлагаем третий путь: отдельной кнопкой на главном экране его
                    // легко не заметить, а выбирается он один раз.
                    pm.SendGump(new MahaonProfessionPickerGump(pm, ProfessionGumpStage.ThirdSelect));
                }

                break;
        }
    }
}
