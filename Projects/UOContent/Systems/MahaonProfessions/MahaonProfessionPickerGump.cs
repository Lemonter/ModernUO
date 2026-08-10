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
    Confirm
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
    private const int ResetCost = 1000; // gold — see CurrencyHelper, 1000 gold = 1,000,000 copper

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

    private static string CategoryName(ProfessionCategory c) => c switch
    {
        ProfessionCategory.Magic   => "Маг",
        ProfessionCategory.Craft   => "Ремесленник",
        ProfessionCategory.Warrior => "Воин",
        ProfessionCategory.Thief   => "Вор",
        ProfessionCategory.Bard    => "Бард",
        ProfessionCategory.Ranger  => "Следопыт",
        _                          => c.ToString()
    };

    protected override void BuildLayout(ref StaticGumpBuilder builder)
    {
        switch (_stage)
        {
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
        var height = current != null ? 340 : 300;

        builder.AddPage();
        builder.AddBackground(0, 0, 320, height, 5054);
        builder.AddAlphaRegion(10, 10, 300, height - 20);

        builder.AddHtml(20, 15, 280, 20, "Камень профессий — выбери путь");

        var y = 45;
        foreach (var category in System.Enum.GetValues<ProfessionCategory>())
        {
            builder.AddButton(20, y, 4005, 4007, 100 + (int)category);
            builder.AddLabel(55, y + 2, 0x480, CategoryName(category));
            y += 28;
        }

        if (current != null)
        {
            y += 10;
            builder.AddHtml(20, y, 280, 20, $"Сейчас: {ProfessionData.GetName(current.Value, _player.Female)}");
            y += 24;
            builder.AddButton(20, y, 4005, 4007, 999);
            builder.AddLabel(55, y + 2, 0x22, $"Сбросить профессию ({ResetCost} золота)");
        }
    }

    private void BuildProfessionSelect(ref StaticGumpBuilder builder)
    {
        var professions = ProfessionData.InCategory(_category);
        var height = 90 + professions.Count * 28;

        builder.AddPage();
        builder.AddBackground(0, 0, 320, height, 5054);
        builder.AddAlphaRegion(10, 10, 300, height - 20);

        builder.AddHtml(20, 15, 280, 20, $"Путь: {CategoryName(_category)}");

        var y = 45;
        foreach (var profession in professions)
        {
            builder.AddButton(20, y, 4005, 4007, 200 + (int)profession);
            builder.AddLabel(55, y + 2, 0x480, ProfessionData.GetName(profession, _player.Female));
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

        var height = 220 + primarySkills.Length * 20;

        builder.AddPage();
        builder.AddBackground(0, 0, 340, height, 5054);
        builder.AddAlphaRegion(10, 10, 320, height - 20);

        builder.AddHtml(20, 15, 300, 20, $"<BASEFONT COLOR=#FFD700>{ProfessionData.GetName(profession, _player.Female)}</BASEFONT>");

        builder.AddHtml(
            20, 40, 300, 20,
            $"Основа: {CategoryName(info.Category)} — доп.: {CategoryName(info.SecondaryCategory)}"
        );

        builder.AddHtml(20, 65, 300, 20, "Пределы характеристик:");
        builder.AddHtml(35, 88, 280, 20, $"Сила: {info.StrCap}   Ловкость: {info.DexCap}   Разум: {info.IntCap}");

        builder.AddHtml(20, 115, 300, 20, $"Навыки класса «{CategoryName(info.Category)}» (потолок 120, у остальных — 100):");

        var y = 138;
        foreach (var skill in primarySkills)
        {
            builder.AddHtml(35, y, 280, 20, $"— {skill}");
            y += 20;
        }

        y += 15;
        builder.AddHtml(
            20, y, 300, 40,
            "Все остальные навыки будут ограничены потолком 100, как у всех, без класса не выше этого."
        );
        y += 45;

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
                    var totalCopper = (long)ResetCost * CurrencyHelper.CopperPerGold;

                    if (pm.Backpack == null || !CurrencyHelper.TryWithdrawCopperValue(pm.Backpack, totalCopper))
                    {
                        pm.SendMessage(0x22, $"Недостаточно золота — нужно {ResetCost}.");
                        pm.SendGump(new MahaonProfessionPickerGump(pm));
                        return;
                    }

                    ProfessionSystem.ClearProfession(pm);
                    pm.SendMessage(0x59, "Профессия сброшена.");
                    return;
                }

                if (buttonId is >= 100 and < 200)
                {
                    var category = (ProfessionCategory)(buttonId - 100);
                    pm.SendGump(new MahaonProfessionPickerGump(pm, ProfessionGumpStage.ProfessionSelect, category));
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
                }

                break;
        }
    }
}
