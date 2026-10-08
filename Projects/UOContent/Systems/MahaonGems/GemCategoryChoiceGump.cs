using Server.Gumps;
using Server.Network;

namespace Server.Systems.MahaonGems;

/// <summary>
///     Shown when a diamond/star sapphire socketer has 100+ Tinkering (see
///     GemSocketingSystem.CanChooseCategory) — lets them steer the roll toward one broad
///     skill category instead of it landing anywhere on the full list. "Как получится"
///     keeps the old fully-random behavior for anyone who'd rather not narrow it.
/// </summary>
public class GemCategoryChoiceGump : Gump
{
    private readonly Mobile _from;
    private readonly Item _item;
    private readonly Item _gem;

    public GemCategoryChoiceGump(Mobile from, Item item, Item gem) : base(80, 80)
    {
        _from = from;
        _item = item;
        _gem = gem;

        Resizable = false;

        var categories = System.Enum.GetValues<GemSkillCategory>();
        var height = 90 + categories.Length * 26 + 40;

        AddPage(0);
        AddBackground(0, 0, 300, height, 5054);
        AddAlphaRegion(10, 10, 280, height - 20);

        AddHtml(20, 15, 260, 40, $"Твоё мастерство ({gem.Name ?? gem.GetType().Name}) позволяет выбрать раздел навыков для бонуса:");

        var y = 60;

        foreach (var category in categories)
        {
            AddButton(20, y, 4005, 4007, 1 + (int)category);
            AddHtml(55, y + 2, 220, 20, GemSocketingSystem.RuCategoryName(category));
            y += 26;
        }

        y += 10;
        AddButton(20, y, 4014, 4016, 100);
        AddHtml(55, y + 2, 220, 20, "Как получится (случайный навык)");
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (_from.Deleted || _item.Deleted || _gem.Deleted)
        {
            return;
        }

        if (info.ButtonID == 100)
        {
            GemSocketingSystem.FinishSocket(_from, _item, _gem, null);
            return;
        }

        var categories = System.Enum.GetValues<GemSkillCategory>();
        var index = info.ButtonID - 1;

        if (index >= 0 && index < categories.Length)
        {
            GemSocketingSystem.FinishSocket(_from, _item, _gem, categories[index]);
        }
    }
}
