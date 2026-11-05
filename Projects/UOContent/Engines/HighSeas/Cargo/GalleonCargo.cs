/// <summary>
///     Груз галеры: настоящий трюм, его палубные «стенки», погрузка и выгрузка.
///
///     Портировано из ServUO (Scripts/Services/Expansions/High Seas/Multis/BaseGalleon.cs — блок
///     фикстур: <c>Fixtures</c>/<c>AddFixtures</c>/<c>AddMainHold</c>/<c>AddHoldItem</c>/<c>AddFixture</c>/
///     <c>RemoveFixture</c>, таблицы <c>HoldIDs</c>/<c>HoldItemIDs</c>, а также <c>DistributeRune</c> из
///     той же части) и из <c>DockMaster.TryRetrieveHold</c>/<c>ShippingCrate.cs</c> — путь «трюм →
///     грузовой ящик».
///
///     Как устроен поток груза:
///     <list type="number">
///         <item>
///             При постройке корабля из мульти берутся маркеры трюма (<c>HoldItemIDs</c>) и его стенок
///             (<c>HoldIDs</c>) и превращаются в <see cref="GalleonHold" /> и <see cref="HoldItem" />.
///             Если клиентских данных нет (пустая установка, тесты) мульти пуст — тогда трюм ставится
///             в центр палубы, чтобы корабль всё равно был грузовым.
///         </item>
///         <item>
///             Предмет, брошенный на палубу корабля (<c>OnDragDrop</c> по мульти), уходит в трюм:
///             в этом движке предметы, лежащие на палубе, не переживают сухой док и затопление, а
///             трюм — это контейнер, который уезжает вместе с кораблём.
///         </item>
///         <item>
///             Сухой док: <c>BaseBoat.EndDryDock</c> в этом движке удаляет корабль вместе с трюмом, а в
///             ServUO корабль уезжал в <c>Map.Internal</c> и груз оставался в его трюме. Поэтому перед
///             удалением груз выгружается владельцу — в банк, затем в рюкзак, затем под ноги
///             (<see cref="UnloadCargoForDryDock" />). Так же поступает и затопление, но там груз
///             по-прежнему идёт на дно: в ServUO <c>OnSink</c> → <c>Delete</c>.
///         </item>
///         <item>
///             Контекстное меню трюма «выгрузить в ящик» (<see cref="RetrieveCargoCrate" />) повторяет
///             <c>DockMaster.TryRetrieveHold</c>: за портовую пошлину груз перекладывается в
///             <see cref="ShipCrate" />, который затем грузится обратно на любой свой корабль.
///         </item>
///         <item>
///             Штатный <c>BaseBoat.Hold</c> у галеры больше не нужен: он спрятан (как и <c>TillerMan</c>),
///             чтобы на палубе не открывались два разных трюма, а его содержимое из старых сейвов
///             переносится в настоящий трюм при загрузке (<see cref="EnsureHold" />).
///         </item>
///     </list>
///
///     Чем отличается от ServUO:
///     <list type="bullet">
///         <item>
///             <c>Fixtures</c> в ServUO был общим списком и содержал ещё штурвал, швартовы и декор
///             палубы (<c>ShipWheel</c>/<c>MooringLine</c>/<c>DeckItem</c>). Здесь список —
///             <c>_holdFixtures</c>, и в нём только трюм и его стенки: остальной декор палубы рисует
///             сам мульти, отдельных предметов для него не заводится.
///         </item>
///         <item>
///             <c>GalleonContainer</c> (переносной сундук палубы с <c>RelocateContainerEntry</c>) не
///             портирован: в ServUO его никто не создаёт — ни дей, ни вендор, ни команда (файл
///             <c>GalleonContainer.cs</c> только объявляет класс), поэтому здесь он был бы мёртвым
///             кодом. Грузовые контейнеры палубы закрывают <see cref="GalleonHold" /> и
///             <see cref="ShipCrate" />.
///         </item>
///         <item>
///             <c>LoadContext</c>/<c>UnloadContext</c> из этапа 3 — это контекстные меню заряжания
///             пушек (<c>Cannon.cs</c>/<c>ShipCannon.cs</c>), а не груза; они уже портированы как
///             <c>TryLoad</c>/<c>TryUnload</c> и живут в <c>Cannons/</c>.
///         </item>
///         <item>
///             <c>ShipRune</c>/<c>MarkRunes</c>/<c>CapturedCaptain</c>/<c>AutoAddCannons</c> остаются
///             вне этапа: рун корабля и пленных капитанов в этом движке нет.
///         </item>
///     </list>
/// </summary>
using System;
using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;
using Server.Multis;

namespace Server.Engines.HighSeas;

public abstract partial class BaseGalleon
{
    /// <summary>Портовая пошлина за выгрузку трюма в ящик (в ServUO — <c>DockMaster.DryDockAmount</c>).</summary>
    public const int CargoRetrieveFee = 2500;

    /// <summary>Дальность, с которой грузовой ящик загружается на корабль (в ServUO — <c>DryDockDistance</c>).</summary>
    public const int CargoLoadDistance = 300;

    /// <summary>Арт стенок трюма по направлениям (в ServUO — <c>HoldIDs</c>).</summary>
    public virtual int[][] HoldIDs => null;

    /// <summary>Арт самого трюма по направлениям (в ServUO — <c>HoldItemIDs</c>).</summary>
    public virtual int[][] HoldItemIDs => null;

    /// <summary>Вместимость трюма в стоунах (в ServUO — <c>GalleonHold.DefaultMaxWeight</c>).</summary>
    public virtual int HoldMaxWeight => 16000;

    /// <summary>Есть ли в трюме груз.</summary>
    [CommandProperty(AccessLevel.GameMaster)]
    public bool HasCargo => _galleonHold?.Deleted == false && _galleonHold.Items.Count > 0;

    /// <summary>Предметы палубы, принадлежащие трюму: сам трюм и его стенки.</summary>
    public List<Item> Fixtures => _holdFixtures;

    /// <summary>Создаёт трюм и его стенки по компонентам мульти (вызывается из конструктора корабля).</summary>
    public void CreateHoldFixtures()
    {
        var components = MultiData.GetComponents(PristineNorthID);
        var north = GetValueForDirection(Direction.North);

        var mainIDs = HoldItemIDs;
        var tileIDs = HoldIDs;

        var mainRow = mainIDs != null && north < mainIDs.Length ? mainIDs[north] : null;
        var tileRow = tileIDs != null && north < tileIDs.Length ? tileIDs[north] : null;

        var hasMainHold = false;

        for (var i = 0; i < components.List.Length; i++)
        {
            var tile = components.List[i];
            var offset = new Point3D(tile.OffsetX, tile.OffsetY, tile.OffsetZ);

            var mainColumn = IndexOfTile(mainRow, tile.ItemId);

            if (mainColumn >= 0)
            {
                AddMainHold(tile.ItemId, offset, mainColumn);
                hasMainHold = true;
                continue;
            }

            var tileColumn = IndexOfTile(tileRow, tile.ItemId);

            if (tileColumn >= 0)
            {
                AddHoldItem(tile.ItemId, offset, tileColumn);
            }
        }

        if (!hasMainHold)
        {
            // Мульти пуст (нет клиентских данных): трюм всё равно нужен — ставим его в центр палубы.
            AddMainHold(mainRow is { Length: > 0 } ? mainRow[0] : 0x3EAE, new Point3D(0, 0, ZSurface), 0);
        }
    }

    /// <summary>Достраивает трюм кораблю, сохранённому до этапа 4 (в сейве поля трюма нет).</summary>
    public void EnsureHold()
    {
        if (_galleonHold?.Deleted != false)
        {
            _galleonHold = null;
            CreateHoldFixtures();
        }

        MigrateClassicHold();
    }

    /// <summary>
    ///     Переносит груз из штатного <c>BaseBoat.Hold</c> в настоящий трюм: до этапа 4 галеры
    ///     пользовались именно им, и такие сейвы не должны потерять груз (корабль в этом движке
    ///     удаляется вместе с классическим трюмом при сухом доке).
    /// </summary>
    private void MigrateClassicHold()
    {
        var legacy = Hold;
        var hold = _galleonHold;

        if (legacy?.Deleted != false || hold?.Deleted != false || legacy.Items.Count == 0)
        {
            return;
        }

        var items = legacy.Items.ToArray();

        for (var i = 0; i < items.Length; i++)
        {
            var item = items[i];

            if (item?.Deleted != false)
            {
                continue;
            }

            if (!hold.TryDropItem(null, item, false) && item.Parent == legacy)
            {
                item.MoveToWorld(GetWorldLocation(), Map);
            }
        }
    }

    /// <summary>Расставляет трюм и его стенки по местам (постановка на воду, смена карты).</summary>
    public void UpdateHoldFixturePositions()
    {
        if (Deleted || Map == null || Map == Map.Internal)
        {
            return;
        }

        var fixtures = _holdFixtures;

        for (var i = 0; i < fixtures.Count; i++)
        {
            var fixture = fixtures[i];

            if (fixture?.Deleted != false || fixture is not IGalleonHoldFixture holdFixture)
            {
                continue;
            }

            fixture.Location = GetPadLocation(holdFixture.HoldOffset);
            holdFixture.UpdateItemID();
        }
    }

    /// <summary>Удаляет трюм и его стенки вместе с кораблём (в ServUO — обход <c>Fixtures</c>).</summary>
    public void DeleteFixtures()
    {
        var fixtures = _holdFixtures.ToArray();

        for (var i = 0; i < fixtures.Length; i++)
        {
            if (fixtures[i]?.Deleted == false)
            {
                fixtures[i].Delete();
            }
        }

        _holdFixtures.Clear();
        _galleonHold = null;
    }

    public void AddFixture(Item item)
    {
        if (item != null && !_holdFixtures.Contains(item))
        {
            _holdFixtures.Add(item);
        }
    }

    public void RemoveFixture(Item item)
    {
        if (item != null)
        {
            _holdFixtures.Remove(item);
        }
    }

    /// <summary>Арт предмета трюма для указанного борта (в ServUO — строки <c>HoldIDs</c> по <c>Facing</c>).</summary>
    public int ResolveHoldFixtureID(int[][] table, int column, Direction facing)
    {
        if (table == null)
        {
            return 0;
        }

        var row = GetValueForDirection(facing);

        if (row < 0 || row >= table.Length)
        {
            return 0;
        }

        var ids = table[row];

        return ids != null && column >= 0 && column < ids.Length ? ids[column] : 0;
    }

    /// <summary>Может ли игрок пользоваться трюмом: офицер и выше (в ServUO — <c>SecurityLevel.Officer</c>).</summary>
    public bool CanAccessHold(Mobile from) =>
        from == null || from.AccessLevel > AccessLevel.Player || GetSecurityLevel(from) >= SecurityLevel.Officer;

    /// <summary>
    ///     Кладёт предмет в трюм. Ошибку «трюм полон» и переполнение по весу озвучивает
    ///     <c>Container.TryDropItem</c> — как при обычном перетаскивании в контейнер.
    /// </summary>
    public bool TryStoreCargo(Mobile from, Item item, bool sendFullMessage = true)
    {
        var hold = _galleonHold;

        if (hold?.Deleted != false || item?.Deleted != false || Deleted)
        {
            return false;
        }

        if (ReferenceEquals(item, this) || ReferenceEquals(item, hold))
        {
            return false;
        }

        if (!CanAccessHold(from))
        {
            from.SendLocalizedMessage(1010436); // You do not have permission to do this.
            return false;
        }

        return hold.TryDropItem(from, item, sendFullMessage);
    }

    /// <summary>
    ///     Предмет, брошенный на палубу, уходит в трюм; карта пути по-прежнему привязывается к кораблю.
    ///     В ServUO такого перехвата не было (там груз просто лежал на палубе), но в этом движке
    ///     палубные предметы не переживают сухой док, поэтому груз должен лежать в контейнере.
    /// </summary>
    public override bool OnDragDrop(Mobile from, Item dropped)
    {
        if (Deleted || dropped?.Deleted != false || from?.Deleted != false)
        {
            return false;
        }

        if (dropped is MapItem map && HasCommandRights(from))
        {
            AssociateMap(map);
            return true;
        }

        if (TryStoreCargo(from, dropped))
        {
            return true;
        }

        return base.OnDragDrop(from, dropped);
    }

    /// <summary>
    ///     Выгружает трюм владельцу перед сухим доком: сначала банк, затем рюкзак, затем земля под
    ///     ногами. Возвращает число выгруженных предметов.
    /// </summary>
    public int UnloadCargoForDryDock(Mobile from)
    {
        var hold = _galleonHold;

        if (hold?.Deleted != false || hold.Items.Count == 0)
        {
            return 0;
        }

        var items = hold.Items.ToArray();
        var moved = 0;
        var stuck = 0;

        for (var i = 0; i < items.Length; i++)
        {
            var item = items[i];

            if (item?.Deleted != false)
            {
                continue;
            }

            if (!GiveToOwner(from, item))
            {
                stuck++;
                continue;
            }

            moved++;
        }

        if (moved > 0)
        {
            from?.SendMessage(0x22, "Груз из трюма переправлен вам: часть — в банк, остальное — в рюкзак.");
        }

        if (stuck > 0)
        {
            from?.SendMessage(0x22, $"Часть груза ({stuck}) переложить не удалось — трюм должен быть пуст.");
        }

        return moved;
    }

    /// <summary>
    ///     Выгружает трюм в грузовой ящик (в ServUO — <c>DockMaster.TryRetrieveHold</c>).
    ///     Возвращает созданный ящик или null, если груза нет или не хватило золота.
    /// </summary>
    public ShipCrate RetrieveCargoCrate(Mobile from, bool chargeFee = true)
    {
        var hold = _galleonHold;

        if (from?.Backpack == null || hold?.Deleted != false || hold.Items.Count == 0)
        {
            from?.SendMessage(0x22, "Трюм пуст.");
            return null;
        }

        // В ServUO докмастер не выдавал второй ящик, пока не вернут первый.
        if (ShipCrate.HasCrateFor(from))
        {
            from.SendLocalizedMessage(1116516); // Thou must return thy current shipping crate...
            return null;
        }

        if (chargeFee && !PayCargoFee(from))
        {
            from.SendLocalizedMessage(1116506, $"{CargoRetrieveFee}"); // The price is ~1_price~...
            return null;
        }

        var crate = new ShipCrate(from, this);
        var items = hold.Items.ToArray();
        var stuck = 0;

        for (var i = 0; i < items.Length; i++)
        {
            var item = items[i];

            if (item?.Deleted != false)
            {
                continue;
            }

            if (!item.Movable)
            {
                stuck++;
                continue;
            }

            crate.DropItem(item);
        }

        if (crate.TotalItems == 0)
        {
            crate.Delete();
            from.SendMessage(0x22, "В трюме нет груза, который можно унести.");
            return null;
        }

        if (from.BankBox?.TryDropItem(from, crate, false) != true)
        {
            crate.MoveToWorld(from.Location, from.Map);
        }

        from.SendLocalizedMessage(1116542, $"{ShipCrate.DecayMinutes}");
        // ^ Yer ship has been unloaded to a crate inside this here warehouse...

        if (stuck > 0)
        {
            from.SendMessage(0x22, "Один или несколько предметов в трюме закреплены и остались на месте.");
        }

        return crate;
    }

    private void AddMainHold(int itemID, Point3D offset, int column)
    {
        var hold = new GalleonHold(this, itemID, offset, column);

        AddFixture(hold);
        hold.MoveToWorld(GetPadLocation(offset), Map);

        _galleonHold = hold;
    }

    private void AddHoldItem(int itemID, Point3D offset, int column)
    {
        var item = new HoldItem(this, itemID, offset, column);

        AddFixture(item);
        item.MoveToWorld(GetPadLocation(offset), Map);
    }

    private static int IndexOfTile(int[] ids, int itemID)
    {
        if (ids == null)
        {
            return -1;
        }

        for (var i = 0; i < ids.Length; i++)
        {
            if (ids[i] == itemID)
            {
                return i;
            }
        }

        return -1;
    }

    private static bool GiveToOwner(Mobile from, Item item)
    {
        if (from == null)
        {
            return false;
        }

        if (from.BankBox?.TryDropItem(from, item, false) == true)
        {
            return true;
        }

        if (from.Backpack?.TryDropItem(from, item, false) == true)
        {
            return true;
        }

        item.MoveToWorld(from.Location, from.Map);
        return true;
    }

    private static bool PayCargoFee(Mobile from)
    {
        if (from.Backpack?.ConsumeTotal(typeof(Gold), CargoRetrieveFee) == true)
        {
            return true;
        }

        return Banker.Withdraw(from, CargoRetrieveFee);
    }
}
