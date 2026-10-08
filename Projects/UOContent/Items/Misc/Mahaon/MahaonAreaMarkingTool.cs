using ModernUO.Serialization;
using Server.Gumps;
using Server.Network;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class MahaonAreaMarkingTool : Item
{
    [Constructible]
    public MahaonAreaMarkingTool() : base(0x1F14)
    {
        Movable = false;
        Name = "Жезл разметки областей (только для ГМ)";
        Hue = 0x481;
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (from.AccessLevel < AccessLevel.GameMaster)
        {
            from.SendMessage("Только ГМ может этим пользоваться.");
            return;
        }

        from.SendGump(new MahaonAreaMenuGump());
    }
}

public class MahaonAreaMenuGump : StaticGump<MahaonAreaMenuGump>
{
    public override bool Singleton => true;
    protected override bool Cached => false;

    public MahaonAreaMenuGump() : base(80, 80)
    {
    }

    protected override void BuildLayout(ref StaticGumpBuilder builder)
    {
        builder.AddPage();
        builder.AddBackground(0, 0, 260, 160, 5054);
        builder.AddAlphaRegion(10, 10, 240, 140);

        builder.AddHtml(20, 15, 220, 20, "Разметка областей");

        builder.AddButton(20, 45, 4005, 4007, 1);
        builder.AddHtml(55, 47, 180, 20, "Отметить новую область");

        builder.AddButton(20, 75, 4005, 4007, 2);
        builder.AddHtml(55, 77, 180, 20, "Список всех областей");

        builder.AddButton(20, 105, 4005, 4007, 3);
        builder.AddHtml(55, 107, 180, 20, "Разметить городской дом");
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var from = sender.Mobile;

        if (from == null || from.AccessLevel < AccessLevel.GameMaster)
        {
            return;
        }

        switch (info.ButtonID)
        {
            case 1:
                from.SendGump(new MahaonAreaKindPickGump());
                break;

            case 2:
                from.SendGump(new MahaonAreaListGump());
                break;

            case 3:
                from.SendMessage(0x59, "Целься по одному в каждый тайл пола дома. Когда закончишь — отмени цель (ESC / правой кнопкой), чтобы перейти к вывеске.");
                from.Target = new AddCityHouseTileTarget(from.Map, new System.Collections.Generic.List<Point3D>());
                break;
        }
    }

    private class AddCityHouseTileTarget : Server.Targeting.Target
    {
        private readonly Map _map;
        private readonly System.Collections.Generic.List<Point3D> _tiles;

        public AddCityHouseTileTarget(Map map, System.Collections.Generic.List<Point3D> tiles) : base(-1, false, Server.Targeting.TargetFlags.None)
        {
            _map = map;
            _tiles = tiles;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted is not IPoint3D ip)
            {
                from.SendMessage(0x22, "Нужно целиться в тайл пола.");
                from.Target = new AddCityHouseTileTarget(_map, _tiles);
                return;
            }

            var loc = new Point3D(ip);

            if (from.Map != _map)
            {
                from.SendMessage(0x22, "Все тайлы дома должны быть на одной карте — этот пропущен.");
            }
            else if (!_tiles.Contains(loc))
            {
                _tiles.Add(loc);
            }

            from.SendMessage(0x59, $"Тайлов отмечено: {_tiles.Count}. Продолжай, или отмени цель, чтобы закончить.");
            from.Target = new AddCityHouseTileTarget(_map, _tiles);
        }

        protected override void OnTargetCancel(Mobile from, Server.Targeting.TargetCancelType cancelType)
        {
            if (_tiles.Count == 0)
            {
                from.SendMessage(0x22, "Разметка отменена — ни один тайл не был отмечен.");
                return;
            }

            from.SendMessage(0x59, $"Тайлов отмечено: {_tiles.Count}. Теперь целься туда, где снаружи должна стоять вывеска.");
            from.Target = new PlaceCityHouseSignTarget(_map, _tiles);
        }
    }

    private class PlaceCityHouseSignTarget : Server.Targeting.Target
    {
        private readonly Map _map;
        private readonly System.Collections.Generic.List<Point3D> _tiles;

        public PlaceCityHouseSignTarget(Map map, System.Collections.Generic.List<Point3D> tiles) : base(-1, true, Server.Targeting.TargetFlags.None)
        {
            _map = map;
            _tiles = tiles;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted is not IPoint3D ip)
            {
                return;
            }

            var loc = new Point3D(ip);
            var label = $"Городской дом #{Systems.MahaonWorld.MahaonCityHouseSystem.All().Count + 1}";

            var house = new MahaonCityHouse(_map, _tiles, label);
            house.ApplyFurnitureHiding();

            var sign = new MahaonCityHouseSign(house);
            sign.MoveToWorld(loc, _map);

            Systems.MahaonWorld.MahaonCityHouseSystem.Register(house);

            from.SendMessage(0x59, $"«{label}» отмечен ({_tiles.Count} тайлов) — вывеска стоит здесь, дом пока свободен. Мебель внутри скрыта и стала проходимой.");
        }
    }
}

public class MahaonAreaKindPickGump : StaticGump<MahaonAreaKindPickGump>
{
    public override bool Singleton => true;
    protected override bool Cached => false;

    public MahaonAreaKindPickGump() : base(80, 80)
    {
    }

    protected override void BuildLayout(ref StaticGumpBuilder builder)
    {
        var kinds = System.Enum.GetValues<MahaonMarkerAreaKind>();

        builder.AddPage();
        builder.AddBackground(0, 0, 280, 70 + kinds.Length * 26, 5054);
        builder.AddAlphaRegion(10, 10, 260, 50 + kinds.Length * 26);

        builder.AddHtml(20, 15, 240, 20, "Тип области — затем укажи два угла");

        var y = 42;

        for (var i = 0; i < kinds.Length; i++)
        {
            builder.AddButton(20, y, 4005, 4007, 10 + i);
            builder.AddHtml(55, y + 2, 200, 20, kinds[i].ToString());
            y += 26;
        }
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var from = sender.Mobile;

        if (from == null || from.AccessLevel < AccessLevel.GameMaster)
        {
            return;
        }

        var kinds = System.Enum.GetValues<MahaonMarkerAreaKind>();
        var index = info.ButtonID - 10;

        if (index < 0 || index >= kinds.Length)
        {
            return;
        }

        var kind = kinds[index];

        from.SendMessage(0x59, $"Тип «{kind}» выбран. Целься в первый угол области.");

        BoundingBoxPicker.Begin(
            from,
            (map, start, end) =>
            {
                var bounds = new Rectangle2D(
                    new Point2D(start.X, start.Y),
                    new Point2D(end.X + 1, end.Y + 1)
                );

                var label = $"{kind} #{Systems.MahaonWorld.MahaonMarkerAreaSystem.All().Count + 1}";
                Systems.MahaonWorld.MahaonMarkerAreaSystem.Create(kind, map, bounds, label);

                from.SendMessage(0x59, $"Область «{label}» создана ({bounds.Width}x{bounds.Height} тайлов на {map}).");
            }
        );
    }
}

public class MahaonAreaListGump : StaticGump<MahaonAreaListGump>
{
    public override bool Singleton => true;
    protected override bool Cached => false;

    public MahaonAreaListGump() : base(60, 60)
    {
    }

    protected override void BuildLayout(ref StaticGumpBuilder builder)
    {
        var areas = Systems.MahaonWorld.MahaonMarkerAreaSystem.All();

        var height = 60 + System.Math.Max(1, areas.Count) * 26;

        builder.AddPage();
        builder.AddBackground(0, 0, 460, height, 5054);
        builder.AddAlphaRegion(10, 10, 440, height - 20);

        builder.AddHtml(20, 15, 420, 20, $"Все отмеченные области ({areas.Count})");

        var y = 42;

        if (areas.Count == 0)
        {
            builder.AddHtml(20, y, 420, 20, "Пока ничего не отмечено.");
        }

        for (var i = 0; i < areas.Count; i++)
        {
            var area = areas[i];

            if (area.Deleted)
            {
                continue;
            }

            builder.AddHtml(
                20, y, 330, 20,
                $"{area.Label} — {area.Kind} — {area.AreaMap} ({area.Bounds.X},{area.Bounds.Y})-({area.Bounds.X + area.Bounds.Width},{area.Bounds.Y + area.Bounds.Height})"
            );

            builder.AddButton(360, y, 4005, 4007, 100 + i); // Телепорт ГМ к области
            builder.AddHtml(385, y + 2, 20, 20, "→");

            builder.AddButton(410, y, 4017, 4019, 200 + i); // Удалить
            builder.AddHtml(430, y + 2, 20, 20, "X");

            y += 26;
        }

        builder.AddButton(20, y + 6, 4014, 4016, 1);
        builder.AddHtml(55, y + 8, 150, 20, "Назад в меню");
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var from = sender.Mobile;

        if (from == null || from.AccessLevel < AccessLevel.GameMaster)
        {
            return;
        }

        if (info.ButtonID == 1)
        {
            from.SendGump(new MahaonAreaMenuGump());
            return;
        }

        var areas = Systems.MahaonWorld.MahaonMarkerAreaSystem.All();

        if (info.ButtonID is >= 100 and < 200)
        {
            var index = info.ButtonID - 100;

            if (index >= 0 && index < areas.Count && !areas[index].Deleted)
            {
                var area = areas[index];
                from.MoveToWorld(new Point3D(area.Bounds.X + area.Bounds.Width / 2, area.Bounds.Y + area.Bounds.Height / 2, from.Z), area.AreaMap);
            }

            from.SendGump(new MahaonAreaListGump());
            return;
        }

        if (info.ButtonID is >= 200 and < 300)
        {
            var index = info.ButtonID - 200;

            if (index >= 0 && index < areas.Count && !areas[index].Deleted)
            {
                Systems.MahaonWorld.MahaonMarkerAreaSystem.Remove(areas[index]);
                from.SendMessage(0x59, "Область удалена.");
            }

            from.SendGump(new MahaonAreaListGump());
        }
    }
}
