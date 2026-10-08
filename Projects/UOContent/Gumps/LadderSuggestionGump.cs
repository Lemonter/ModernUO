using System;
using Server.Items;
using Server.Network;
using Server.Systems.MahaonMining;

namespace Server.Gumps;

public class LadderSuggestionGump : StaticGump<LadderSuggestionGump>
{
    private readonly Mobile _from;
    private readonly Point3D _loc;
    private readonly Point3D _center;
    private readonly Map _map;
    private readonly Action _onDecline;

    public override bool Singleton => false;
    protected override bool Cached => false;

    public LadderSuggestionGump(Mobile from, Point3D loc, Direction facing, Point3D center, Map map, Action onDecline) : base(150, 150)
    {
        _from = from;
        _loc = loc;
        _center = center;
        _map = map;
        _onDecline = onDecline;
    }

    protected override void BuildLayout(ref StaticGumpBuilder builder)
    {
        builder.AddPage();
        builder.AddBackground(0, 0, 300, 150, 5054);
        builder.AddAlphaRegion(10, 10, 280, 130);

        builder.AddHtml(20, 20, 260, 60, "Руда здесь кончилась. Сделать лестницу вглубь, используя долото?");

        builder.AddButton(30, 90, 4005, 4007, 1);
        builder.AddHtml(65, 92, 100, 20, "Да");

        builder.AddButton(160, 90, 4005, 4007, 0);
        builder.AddHtml(195, 92, 100, 20, "Нет");
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (_from.Deleted || _from.Map != _map)
        {
            return;
        }

        if (info.ButtonID != 1 || _from.Backpack?.FindItemByType<MahaonChisel>() == null)
        {
            if (info.ButtonID == 1)
            {
                _from.SendMessage("Долота больше нет в рюкзаке.");
            }

            _onDecline?.Invoke();
            return;
        }

        // Auto-picking from whichever way the player happened to be facing kept coming out
        // crooked — just ask directly instead.
        _from.SendGump(new LadderDirectionGump(_from, _loc, _center, _map));
    }
}
