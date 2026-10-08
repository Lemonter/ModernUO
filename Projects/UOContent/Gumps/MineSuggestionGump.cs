using Server.Network;
using Server.Systems.MahaonMining;

namespace Server.Gumps;

public class MineSuggestionGump : StaticGump<MineSuggestionGump>
{
    private readonly Mobile _from;
    private readonly Point3D _entranceLoc;
    private readonly Point3D _wallLoc;
    private readonly Direction _facing;
    private readonly Map _map;

    public override bool Singleton => false;

    public MineSuggestionGump(Mobile from, Point3D entranceLoc, Point3D wallLoc, Direction facing, Map map) : base(150, 150)
    {
        _from = from;
        _entranceLoc = entranceLoc;
        _wallLoc = wallLoc;
        _facing = facing;
        _map = map;
    }

    protected override void BuildLayout(ref StaticGumpBuilder builder)
    {
        builder.AddPage();
        builder.AddBackground(0, 0, 300, 150, 5054);
        builder.AddAlphaRegion(10, 10, 280, 130);

        builder.AddHtml(20, 20, 260, 60, "Здесь больше нет руды. Начать в этом месте настоящую шахту?");

        builder.AddButton(30, 90, 4005, 4007, 1);
        builder.AddHtml(65, 92, 100, 20, "Да");

        builder.AddButton(160, 90, 4005, 4007, 0);
        builder.AddHtml(195, 92, 100, 20, "Нет");
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (info.ButtonID != 1 || _from.Deleted || _from.Map != _map)
        {
            return;
        }

        if (MineComplexSystem.HasEntranceAt(_entranceLoc, _map))
        {
            _from.SendMessage("Здесь уже есть шахта.");
            return;
        }

        if (!MineComplexSystem.IsWallSegmentStraightEnough(_map, _wallLoc, _facing))
        {
            _from.SendMessage("Стена здесь неровная — тут не заложить нормальный вход в шахту.");
            return;
        }

        MineComplexSystem.CreateMine(_from, _entranceLoc, _facing, _map);
    }
}
