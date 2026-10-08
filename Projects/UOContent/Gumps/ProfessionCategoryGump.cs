using Server.Network;
using Server.Systems.MahaonProfessions;

namespace Server.Gumps;

public class ProfessionCategoryGump : StaticGump<ProfessionCategoryGump>
{
    private readonly Mobile _player;

    public override bool Singleton => true;

    private static readonly (ProfessionCategory category, string label)[] Categories =
    {
        (ProfessionCategory.Magic, "Магические"),
        (ProfessionCategory.Craft, "Ремесленника"),
        (ProfessionCategory.Warrior, "Воинские"),
        (ProfessionCategory.Thief, "Воровские"),
        (ProfessionCategory.Bard, "Барда"),
        (ProfessionCategory.Ranger, "Рейнджера")
    };

    public ProfessionCategoryGump(Mobile player) : base(50, 50) => _player = player;

    protected override void BuildLayout(ref StaticGumpBuilder builder)
    {
        var height = 60 + Categories.Length * 25;

        builder.AddPage();
        builder.AddBackground(0, 0, 280, height, 5054);
        builder.AddAlphaRegion(10, 10, 260, height - 20);
        builder.AddHtml(15, 15, 250, 20, "Выбери направление:");

        for (var i = 0; i < Categories.Length; i++)
        {
            var y = 45 + i * 25;
            builder.AddButton(15, y, 4005, 4007, i + 1);
            builder.AddLabelPlaceholder(45, y, 0x480, $"cat{i}");
        }
    }

    protected override void BuildStrings(ref GumpStringsBuilder builder)
    {
        for (var i = 0; i < Categories.Length; i++)
        {
            builder.SetStringSlot($"cat{i}", Categories[i].label);
        }
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var index = info.ButtonID - 1;
        if (index < 0 || index >= Categories.Length)
        {
            return;
        }

        _player.SendGump(new ProfessionPickGump(_player, Categories[index].category));
    }
}
