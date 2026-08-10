using System.Collections.Generic;
using Server.Mobiles;
using Server.Network;
using Server.Systems.MahaonProfessions;

namespace Server.Gumps;

public class ProfessionPickGump : DynamicGump
{
    private readonly PlayerMobile _player;
    private readonly List<MahaonProfession> _professions;

    public override bool Singleton => true;

    public ProfessionPickGump(Mobile player, ProfessionCategory category) : base(50, 50)
    {
        _player = (PlayerMobile)player;
        _professions = ProfessionData.InCategory(category);
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        var height = 60 + _professions.Count * 25;

        builder.AddPage();
        builder.AddBackground(0, 0, 300, height, 5054);
        builder.AddAlphaRegion(10, 10, 280, height - 20);
        builder.AddHtml(15, 15, 270, 20, "Выбери профессию (это навсегда):");

        var current = ProfessionSystem.GetProfession(_player);

        for (var i = 0; i < _professions.Count; i++)
        {
            var y = 45 + i * 25;
            var profession = _professions[i];
            var name = ProfessionData.GetName(profession, _player.Female);
            var hue = current == profession ? 0x59 : 0x480;

            builder.AddButton(15, y, 4005, 4007, i + 1);
            builder.AddLabel(45, y, hue, name);
        }
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var index = info.ButtonID - 1;
        if (index < 0 || index >= _professions.Count)
        {
            return;
        }

        var profession = _professions[index];

        if (ProfessionSystem.GetProfession(_player) != null)
        {
            _player.SendMessage(0x22, "Профессия выбирается только один раз — у тебя уже есть выбор.");
            return;
        }

        ProfessionSystem.SetProfession(_player, profession);
        _player.SendMessage(0x59, $"Ты выбрал профессию: {ProfessionData.GetName(profession, _player.Female)}.");
    }
}
