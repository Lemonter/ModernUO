using System.Collections.Generic;
using Server.Commands;
using Server.Items;
using Server.Targeting;

namespace Server.Systems.MahaonWorld;

/// <summary>
/// Changing a city house's floor after it is marked:
///   [CityHouseAddTiles    — target the house's sign, then floor tiles; cancel to finish.
///   [CityHouseRemoveTiles — the same, taking tiles off. Fixed items there are let go, furniture
///                           goes back to the owner as deeds.
/// </summary>
public static class MahaonCityHouseCommands
{
    public static void Configure()
    {
        CommandSystem.Register("CityHouseAddTiles", AccessLevel.GameMaster, e => Begin(e.Mobile, true));
        CommandSystem.Register("CityHouseRemoveTiles", AccessLevel.GameMaster, e => Begin(e.Mobile, false));
    }

    private static void Begin(Mobile from, bool add)
    {
        from.SendMessage("Укажи вывеску городского дома.");
        from.BeginTarget(
            -1,
            false,
            TargetFlags.None,
            (m, targeted) =>
            {
                if (targeted is MahaonCityHouseSign { House: { Deleted: false } house })
                {
                    m.SendMessage(add ? "Указывай плитки, которые добавить. Отмена — закончить." : "Указывай плитки, которые убрать. Отмена — закончить.");
                    m.Target = new TileTarget(house, add, []);
                }
                else
                {
                    m.SendMessage(0x22, "Это не вывеска городского дома.");
                }
            }
        );
    }

    private class TileTarget : Target
    {
        private readonly MahaonCityHouse _house;
        private readonly bool _add;
        private readonly List<Point3D> _tiles;

        public TileTarget(MahaonCityHouse house, bool add, List<Point3D> tiles) : base(-1, true, TargetFlags.None)
        {
            _house = house;
            _add = add;
            _tiles = tiles;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted is IPoint3D p && from.Map == _house.AreaMap)
            {
                var tile = new Point3D(p);
                if (!_tiles.Contains(tile))
                {
                    _tiles.Add(tile);
                }

                from.SendMessage($"Плиток: {_tiles.Count}.");
            }

            from.Target = new TileTarget(_house, _add, _tiles);
        }

        protected override void OnTargetCancel(Mobile from, TargetCancelType cancelType)
        {
            if (_tiles.Count == 0 || _house.Deleted)
            {
                return;
            }

            var changed = _add ? _house.AddTiles(_tiles) : _house.RemoveTiles(_tiles);
            from.SendMessage(0x59, $"«{_house.Label}»: {(_add ? "добавлено" : "убрано")} плиток — {changed}, всего {_house.Tiles.Count}.");
        }
    }
}
