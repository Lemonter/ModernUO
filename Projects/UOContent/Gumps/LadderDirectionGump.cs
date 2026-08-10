using Server.Network;
using Server.Systems.MahaonMining;

namespace Server.Gumps;

public class LadderDirectionGump : StaticGump<LadderDirectionGump>
{
    private readonly Mobile _from;
    private readonly Point3D _loc;
    private readonly Point3D _center;
    private readonly Map _map;

    public override bool Singleton => false;
    protected override bool Cached => false;

    public LadderDirectionGump(Mobile from, Point3D loc, Point3D center, Map map) : base(150, 150)
    {
        _from = from;
        _loc = loc;
        _center = center;
        _map = map;
    }

    protected override void BuildLayout(ref StaticGumpBuilder builder)
    {
        builder.AddPage();
        builder.AddBackground(0, 0, 260, 220, 5054);
        builder.AddAlphaRegion(10, 10, 240, 200);

        builder.AddHtml(20, 20, 220, 20, "Куда развернуть лестницу?");

        builder.AddButton(30, 55, 4005, 4007, 1);
        builder.AddHtml(65, 57, 150, 20, "Север");

        builder.AddButton(30, 85, 4005, 4007, 2);
        builder.AddHtml(65, 87, 150, 20, "Юг");

        builder.AddButton(30, 115, 4005, 4007, 3);
        builder.AddHtml(65, 117, 150, 20, "Восток");

        builder.AddButton(30, 145, 4005, 4007, 4);
        builder.AddHtml(65, 147, 150, 20, "Запад");

        builder.AddButton(30, 180, 4005, 4007, 0);
        builder.AddHtml(65, 182, 150, 20, "Отмена");
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (_from.Deleted || _from.Map != _map || info.ButtonID is 0 or < 0 or > 4)
        {
            return;
        }

        var facing = info.ButtonID switch
        {
            1 => Direction.North,
            2 => Direction.South,
            3 => Direction.East,
            4 => Direction.West,
            _ => Direction.South
        };

        MineComplexSystem.BuildLadder(_from, _loc, facing, _center, _map);
    }
}
