/// <summary>
///     Портировано из ServUO (Scripts/Services/Expansions/High Seas/Multis/BaseGalleon.cs — блок
///     <c>Cannons</c>: <c>MaxCannons</c>, <c>WeaponPad</c>, <c>CannonTileIDs</c>, <c>TryAddCannon</c>,
///     <c>RemoveCannon</c>, <c>IsValidCannonSpot</c>, <c>GetCannonPosition</c>, <c>UpdateCannonID(s)</c>,
///     <c>CannonDamageMod</c>).
///
///     Орудийная палуба галеры: список пушек, орудийные пятаки (создаются из компонентов мульти),
///     постановка/снятие пушки и пересчёт её арта под текущий борт и направление корабля.
///
///     Адаптация под этот движок:
///     <list type="bullet">
///         <item>
///             Орудийные пятаки и пушки хранятся в <c>[SerializableField]</c>-списках самой галеры
///             (в ServUO пятаки лежали в общем списке фикстур, а тот появится только на этапе 4).
///             Поля объявлены в <c>BaseGalleon.cs</c> — генератор сериализации видит атрибуты только
///             в файле с <c>[SerializationGenerator]</c>.
///         </item>
///         <item>
///             Место пушки задаётся смещением в «северной» системе координат (<c>PadOffset</c>), а не
///             готовыми координатами: тогда поворот корабля, смена арта повреждённого корпуса и
///             переезд на другую карту пересчитываются одной формулой <see cref="GetPadLocation" />.
///         </item>
///         <item>
///             Пушки и пятаки ездят вместе с кораблём штатным <c>BaseBoat.GetMovingEntities</c>
///             (они видны и лежат в границах мульти), а абсолютно переставляются только при смене
///             карты — как кормчий. Своего <c>OnLocationChange</c> у палубы нет намеренно: иначе
///             пушки уезжали бы вдвое за каждый шаг корабля.
///         </item>
///         <item>
///             <c>GetCannonPosition</c> не расписан по каждой галере (в ServUO это отдельные switch
///             на 40 строк с пиксельными порогами), а считается геометрически: нос/корма — по
///             продольному смещению относительно трети корпуса, борт — по поперечному. Порядок
///             значений <see cref="ShipPosition" /> и окно пушки от этого не меняются.
///         </item>
///         <item>
///             <c>DryDockResult.Cannon</c> и <c>DryDockResult.Damaged</c> в перечислении этого движка
///             отсутствуют, поэтому запреты выдаются сообщениями до вызова штатного кода (см.
///             <c>BaseGalleon.BeginDryDock</c>/<c>CompleteDryDock</c>).
///         </item>
///         <item>
///             В ServUO сухой док увозил корабль в <c>Map.Internal</c> вместе с пушками. В этом
///             движке <c>BaseBoat.EndDryDock</c> корабль удаляет, поэтому пушки возвращаются
///             владельцу деями (см. <see cref="TakeCannonDeeds" />).
///         </item>
///     </list>
/// </summary>
using System;
using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.HighSeas;

public abstract partial class BaseGalleon
{
    /// <summary>Арт пушек: строка — направление ствола, столбец — класс пушки.</summary>
    private static readonly int[][] _cannonIDs =
    [
        [16918, 16922, 41664, 41979], // South
        [16919, 16923, 41665, 41980], // West
        [16920, 16924, 41666, 41981], // North
        [16921, 16925, 41667, 41982]  // East
    ];

    /// <summary>Кэш «северных» смещений орудийных пятаков (считается по данным мульти).</summary>
    private Point3D[] _padOffsets;

    /// <summary>ID целого северного корпуса: у повреждённого мульти другие компоненты.</summary>
    private int PristineNorthID => NorthID - DamageValue * 4;

    /// <summary>Сколько пушек держит палуба (в ServUO — <c>MaxCannons</c>).</summary>
    public virtual int MaxCannons => 0;

    /// <summary>Множитель урона пушек этого корпуса (в ServUO — <c>CannonDamageMod</c>).</summary>
    public virtual double CannonDamageMod => 1.0;

    /// <summary>
    ///     Тайлы орудийных пятаков по направлениям (в ServUO — <c>CannonTileIDs</c>). Строки
    ///     повторяются для повреждённых корпусов; здесь достаточно пристиновых — пятаки создаются
    ///     один раз по северному мульти и дальше не пересоздаются.
    /// </summary>
    public virtual int[][] CannonTileIDs => null;

    public static int[][] CannonIDs => _cannonIDs;

    /// <summary>Пушки на палубе (генерируемое свойство списка из <c>BaseGalleon.cs</c>).</summary>
    [CommandProperty(AccessLevel.GameMaster)]
    public int CannonCount => Cannons?.Count ?? 0;

    /// <summary>Стоит ли на этой клетке палубы пушка.</summary>
    public bool IsCannonAt(int x, int y)
    {
        var cannons = Cannons;

        if (cannons == null)
        {
            return false;
        }

        for (var i = 0; i < cannons.Count; i++)
        {
            var cannon = cannons[i];

            if (cannon?.Deleted == false && cannon.X == x && cannon.Y == y)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Строка таблицы арта для направления (в ServUO — <c>GetValueForDirection</c>).</summary>
    public static int GetValueForDirection(Direction direction) => direction switch
    {
        Direction.South => 0,
        Direction.West  => 1,
        Direction.North => 2,
        Direction.East  => 3,
        _               => 2
    };

    /// <summary>Пересчитывает арт всех пушек (после поворота корабля).</summary>
    public void UpdateCannonIDs()
    {
        var cannons = Cannons;

        if (cannons == null)
        {
            return;
        }

        for (var i = 0; i < cannons.Count; i++)
        {
            UpdateCannonID(cannons[i]);
        }
    }

    /// <summary>Ставит пушке арт под её текущее направление (в ServUO — <c>UpdateCannonID</c>).</summary>
    public void UpdateCannonID(Item cannon)
    {
        if (cannon?.Deleted != false || cannon is not IShipCannon shipCannon)
        {
            return;
        }

        var type = (int)shipCannon.Power;

        cannon.ItemID = _cannonIDs[GetValueForDirection(shipCannon.Facing)][type];
    }

    /// <summary>
    ///     Абсолютная точка палубного смещения в текущем развороте корабля (в ServUO — прямые
    ///     координаты из мульти). <c>padOffset</c> задан в «северной» системе координат.
    /// </summary>
    public Point3D GetPadLocation(Point3D padOffset)
    {
        var rotated = GetRotatedLocation(padOffset.X, padOffset.Y);

        return new Point3D(rotated.X, rotated.Y, rotated.Z + padOffset.Z);
    }

    /// <summary>Переводит мировую точку в «северное» смещение от центра корабля (обратный поворот).</summary>
    public Point3D ToNorthOffset(Point3D world)
    {
        var rx = world.X - X;
        var ry = world.Y - Y;

        var count = (int)Facing / 2;

        for (var i = 0; i < count; i++)
        {
            var temp = rx;
            rx = ry;
            ry = -temp;
        }

        return new Point3D(rx, ry, world.Z - Z);
    }

    /// <summary>«Северные» смещения всех орудийных пятаков корпуса.</summary>
    public Point3D[] GetPadOffsets()
    {
        if (_padOffsets != null)
        {
            return _padOffsets;
        }

        var ids = CannonTileIDs;

        if (ids == null || MaxCannons <= 0)
        {
            return _padOffsets = [];
        }

        var pads = new List<Point3D>();
        var components = MultiData.GetComponents(PristineNorthID);

        for (var i = 0; i < components.List.Length; i++)
        {
            var tile = components.List[i];

            if (IsWeaponPadTile(ids, tile.ItemId))
            {
                pads.Add(new Point3D(tile.OffsetX, tile.OffsetY, tile.OffsetZ));
            }
        }

        return _padOffsets = pads.ToArray();
    }

    private static bool IsWeaponPadTile(int[][] ids, int itemID)
    {
        for (var row = 0; row < ids.Length; row++)
        {
            var list = ids[row];

            for (var i = 0; i < list.Length; i++)
            {
                if (list[i] == itemID)
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>Создаёт орудийные пятаки по компонентам мульти (вызывается из конструктора корабля).</summary>
    public void CreateWeaponPads()
    {
        var offsets = GetPadOffsets();

        if (offsets.Length == 0)
        {
            return;
        }

        for (var i = 0; i < offsets.Length; i++)
        {
            var pad = new WeaponPad(GetPadItemID(offsets[i]), this, offsets[i]);
            pad.MoveToWorld(GetPadLocation(offsets[i]), Map);

            WeaponPads.Add(pad);
        }
    }

    /// <summary>Тайл пятака — из северного мульти; для отсутствующей записи берём первый попавшийся.</summary>
    private int GetPadItemID(Point3D padOffset)
    {
        var components = MultiData.GetComponents(PristineNorthID);
        var ids = CannonTileIDs;

        for (var i = 0; i < components.List.Length; i++)
        {
            var tile = components.List[i];

            if (tile.OffsetX == padOffset.X && tile.OffsetY == padOffset.Y && IsWeaponPadTile(ids, tile.ItemId))
            {
                return tile.ItemId;
            }
        }

        return 0x2198;
    }

    /// <summary>
    ///     Старые корабли (сейвы этапов 1–2) пришли без пятаков — достраиваем их при загрузке, иначе
    ///     на такой галере некуда поставить пушку.
    /// </summary>
    public void EnsureWeaponPads()
    {
        if (MaxCannons > 0 && WeaponPads.Count == 0 && GetPadOffsets().Length > 0)
        {
            CreateWeaponPads();
        }
    }

    /// <summary>Расставляет пятаки и пушки по местам при смене карты (постановка на воду).</summary>
    public void UpdateCannonPositions()
    {
        if (Deleted || Map == null || Map == Map.Internal)
        {
            return;
        }

        var pads = WeaponPads;

        for (var i = 0; i < pads.Count; i++)
        {
            if (pads[i] is WeaponPad pad && !pad.Deleted)
            {
                pad.Location = GetPadLocation(pad.PadOffset);
            }
        }

        var cannons = Cannons;

        for (var i = 0; i < cannons.Count; i++)
        {
            var cannon = cannons[i];

            if (cannon?.Deleted != false || cannon is not IShipCannon shipCannon)
            {
                continue;
            }

            cannon.Location = GetPadLocation(shipCannon.PadOffset);
        }
    }

    /// <summary>Постановка пушки по дею: сначала проверка «у берега или причала», как в ServUO.</summary>
    public bool TryAddCannon(Mobile from, Point3D pnt, ShipCannonDeed deed, bool force = false)
    {
        if (deed == null)
        {
            return false;
        }

        if (!force && !IsNearLandOrDocks(this))
        {
            from?.SendLocalizedMessage(1116076); // The ship must be near shore or a sea market...
            return false;
        }

        IShipCannon cannon = deed.CannonType switch
        {
            CannonPower.Heavy   => new Carronade(this),
            CannonPower.Massive => new Blundercannon(this),
            _                   => new Culverin(this)
        };

        return TryAddCannon(from, pnt, cannon, deed, force);
    }

    /// <summary>Постановка готового предмета-пушки на палубу.</summary>
    public bool TryAddCannon(Mobile from, Point3D pnt, IShipCannon cannon, ShipCannonDeed deed, bool force = false)
    {
        if (cannon is not Item item || Deleted)
        {
            return false;
        }

        if (Cannons.Count >= MaxCannons)
        {
            from?.SendMessage(0x22, "На палубе нет свободных орудийных мест.");
            item.Delete();
            return false;
        }

        if (!IsValidCannonSpot(ref pnt, from, force, out var padOffset))
        {
            item.Delete();
            return false;
        }

        item.MoveToWorld(pnt, Map);

        cannon.PadOffset = padOffset;
        Cannons.Add(item);

        UpdateCannonID(item);
        cannon.Position = GetCannonPosition(pnt);

        DoAreaMessage(1116074, from); // ~1_NAME~ deploys a ship cannon.

        if (deed != null && (from == null || from.AccessLevel == AccessLevel.Player))
        {
            deed.Delete();
        }

        return true;
    }

    /// <summary>Можно ли поставить пушку в эту точку (в ServUO — <c>IsValidCannonSpot</c>).</summary>
    public bool IsValidCannonSpot(ref Point3D pnt, Mobile from, bool force, out Point3D padOffset)
    {
        padOffset = Point3D.Zero;

        if (Deleted)
        {
            return false;
        }

        if (IsCannonAt(pnt.X, pnt.Y))
        {
            from?.SendLocalizedMessage(1116075); // There is already a weapon deployed here.
            return false;
        }

        var offsets = GetPadOffsets();

        for (var i = 0; i < offsets.Length; i++)
        {
            var location = GetPadLocation(offsets[i]);

            if (location.X != pnt.X || location.Y != pnt.Y)
            {
                continue;
            }

            if (Map == null || Map == Map.Internal)
            {
                // Корабль ещё не поставлен на воду: проверять, свободен ли пятак, не по чему.
                padOffset = offsets[i];
                pnt = location;
                return true;
            }

            foreach (var mob in Map.GetMobilesInRange<Mobile>(location, 0))
            {
                if (mob.Deleted || mob.Hidden || mob.AccessLevel != AccessLevel.Player)
                {
                    continue;
                }

                from?.SendMessage(0x22, "Пятак занят — освободите его.");
                return false;
            }

            padOffset = offsets[i];
            padOffset = new Point3D(
                padOffset.X,
                padOffset.Y,
                padOffset.Z + TileData.ItemTable[GetPadItemID(offsets[i]) & TileData.MaxItemValue].CalcHeight
            );

            pnt = GetPadLocation(padOffset);

            return true;
        }

        // Принудительная постановка (ГМ, тесты, достройка старого корабля): пятак не ищем.
        if (force)
        {
            padOffset = ToNorthOffset(pnt);
            return true;
        }

        from?.SendLocalizedMessage(1116626); // You must use this on a ship weapon pad.

        return false;
    }

    /// <summary>Снимает пушку со списка (её удаление вызывает сама пушка).</summary>
    public void RemoveCannon(Item cannon) => Cannons?.Remove(cannon);

    /// <summary>Удаляет пушки и пятаки вместе с кораблём (в ServUO — <c>OnAfterDelete</c>).</summary>
    public void DeleteCannons()
    {
        var cannons = Cannons.ToArray();

        for (var i = 0; i < cannons.Length; i++)
        {
            if (cannons[i]?.Deleted == false)
            {
                cannons[i].Delete();
            }
        }

        Cannons.Clear();

        var pads = WeaponPads.ToArray();

        for (var i = 0; i < pads.Length; i++)
        {
            if (pads[i]?.Deleted == false)
            {
                pads[i].Delete();
            }
        }

        WeaponPads.Clear();
    }

    /// <summary>
    ///     Прячет палубные предметы на время проверки сухого дока: <c>BaseBoat.CheckDryDock</c> считает
    ///     помехой любой видимый предмет на палубе, а пушки и пятаки — всегда на палубе.
    /// </summary>
    public void SetCannonDeckVisible(bool visible)
    {
        var cannons = Cannons;

        for (var i = 0; i < cannons.Count; i++)
        {
            if (cannons[i]?.Deleted == false && cannons[i].Visible != visible)
            {
                cannons[i].Visible = visible;
            }
        }

        var pads = WeaponPads;

        for (var i = 0; i < pads.Count; i++)
        {
            if (pads[i]?.Deleted == false && pads[i].Visible != visible)
            {
                pads[i].Visible = visible;
            }
        }
    }

    /// <summary>Заряженные пушки не пускают корабль в сухой док (в ServUO — <c>DryDockResult.Cannon</c>).</summary>
    public bool HasLoadedCannons()
    {
        var cannons = Cannons;

        for (var i = 0; i < cannons.Count; i++)
        {
            if (cannons[i] is IShipCannon { AmmoType: not AmmunitionType.Empty })
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     Деи пушек для возврата владельцу при сухом доке: корабль в этом движке удаляется, поэтому
    ///     пушки не могут пережить док «на борту», как в ServUO.
    /// </summary>
    public List<ShipCannonDeed> TakeCannonDeeds()
    {
        var deeds = new List<ShipCannonDeed>();
        var cannons = Cannons;

        for (var i = 0; i < cannons.Count; i++)
        {
            if (cannons[i] is IShipCannon cannon && cannon.GetDeed is { } deed)
            {
                deeds.Add(deed);
            }
        }

        return deeds;
    }

    /// <summary>Сообщение всем игрокам на борту (в ServUO — <c>DoAreaMessage</c> пушки).</summary>
    public void DoAreaMessage(int cliloc, Mobile from)
    {
        var map = Map;

        if (map == null || map == Map.Internal || from == null)
        {
            return;
        }

        foreach (var mob in map.GetMobilesInRange<PlayerMobile>(Location, 18))
        {
            if (mob.Deleted || mob == from || !Contains(mob))
            {
                continue;
            }

            mob.SendLocalizedMessage(cliloc, from.Name);
        }
    }

    /// <summary>
    ///     Место пушки на палубе (в ServUO — <c>GetCannonPosition</c> с отдельным switch на каждый
    ///     корпус). Смещение переводится в «северную» систему: нос — отрицательные Y, левый борт —
    ///     отрицательные X.
    /// </summary>
    public ShipPosition GetCannonPosition(Point3D pnt)
    {
        var offset = ToNorthOffset(pnt);
        var bounds = GetHullComponents();

        var bowLimit = bounds.Min.Y + Math.Max(1, bounds.Height / 3);
        var aftLimit = bounds.Max.Y - Math.Max(1, bounds.Height / 3);

        if (offset.Y <= bowLimit)
        {
            return offset.X switch
            {
                < 0 => ShipPosition.BowPort,
                > 0 => ShipPosition.BowStarboard,
                _   => ShipPosition.Bow
            };
        }

        if (offset.Y >= aftLimit)
        {
            return offset.X switch
            {
                < 0 => ShipPosition.AftPort,
                > 0 => ShipPosition.AftStarboard,
                _   => ShipPosition.Aft
            };
        }

        return offset.X < 0 ? ShipPosition.AmidShipPort : ShipPosition.AmidShipStarboard;
    }
}
