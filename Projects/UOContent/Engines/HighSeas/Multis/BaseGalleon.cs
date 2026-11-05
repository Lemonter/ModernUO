/// <summary>
///     Портировано из ServUO (Scripts/Services/Expansions/High Seas/Multis/BaseGalleon.cs).
///
///     Корабль-галера High Seas: корпус (мульти 0x18/0x24/0x30/0x40 и их повороты), NPC-кормчий,
///     список доступа (<see cref="ShipAccessList" />) и окраска корпуса. Ходит на штатном движке
///     этого репозитория: <see cref="BaseBoat.StartMove(Direction, bool)" />,
///     <see cref="BaseBoat.StartTurn" />/<see cref="BaseBoat.Facing" /> — то же, чем управляются
///     классические лодки.
///
///     Адаптация под этот движок (в ServUO <c>BaseBoat</c> был другим):
///     <list type="bullet">
///         <item>
///             <c>BaseBoat</c> здесь — <c>BaseMulti</c>, а не <c>IMount</c>; никакого mouse-pilot
///             пакета 0x33 и <c>BoatMountItem</c> нет и не появится. Руль — обычные речевые команды
///             «вперёд/назад/влево/вправо/стоп» (keyword-ы клиента 0x45–0x4F, как у классических
///             лодок, плюс русские слова — см. <see cref="GalleonPilot" />).
///         </item>
///         <item>
///             <c>TillerMan</c> в этом движке — предмет, а не мобил: он остаётся в мире (на нём держатся
///             речевые команды <c>BaseBoat</c> и табличка состояния, а <c>Turn</c> обращается к нему без
///             проверки на null), но у галеры он скрыт — видимый рулевой один, <see cref="GalleonPilot" />,
///             и он же озвучивает приказы и отказы.
///         </item>
///         <item>
///             <c>BaseBoat.CheckDryDock</c> считает помехой любого моба на палубе, поэтому сухой док
///             перекрыт (<c>new BeginDryDock</c>): кормчий на время синхронной проверки убирается с
///             карты (см. <see cref="BeginDryDock" />).
///         </item>
///         <item>
///             Кормчий-мобил ходит вместе с кораблём через <c>BaseBoat.GetMovingEntities</c>, а
///             переставляется абсолютно только при смене карты (<see cref="OnMapChange" />) — то есть
///             при постановке на воду. Прямой перенос корабля ГМом через <c>Location</c> кормчего не
///             тянет: он останется на прежних координатах.
///         </item>
///         <item>
///             <c>ZSurface</c> в <c>BaseBoat</c> нет — заведена своя виртуальная высота палубы,
///             чтобы поставить кормчего на палубу, а не на уровень воды.
///         </item>
///         <item>
///             Список доступа вынесен в <see cref="HighSeasShips" /> (GenericPersistence по серийнику
///             корабля): на корабле остались только <c>[SerializableField]</c>-поля.
///         </item>
///         <item>
///             <c>PaintComponents</c> в ServUO перекрашивал ещё и фикстуры палубы (штурвал, швартовы,
///             трюм) — эти предметы не портированы (этапы 3–4), поэтому красится только сам корпус.
///         </item>
///     </list>
///
///     Повреждения корпуса (этап 2) вынесены в partial-часть <c>GalleonDamage.cs</c>:
///     <c>Hits</c>/<c>MaxHits</c>/<c>Durability</c>, уровни <see cref="DamageLevel" />, аварийный арт
///     мульти (±4 к ID корпуса), аварийные заделки и постоянный ремонт, затопление ниже
///     <see cref="ScuttleLevel" />, <c>GetMovementInterval</c> и <c>IsNearLandOrDocks</c>. Там же —
///     отказ в сухом доке повреждённому корпусу: в ServUO это <c>DryDockResult.Damaged</c>, а в
///     перечислении этого движка такого значения нет.
///
///     Сознательно оставлено за пределами этапа 4 (в этом движке соответствующих API нет):
///     <list type="bullet">
///         <item>
///             <c>GalleonContainer</c> (переносной сундук палубы с <c>RelocateContainerEntry</c>) — в ServUO
///             его никто не создаёт, поэтому класс не портирован; груз закрывают <c>GalleonHold</c> и
///             <c>ShipCrate</c> (см. <c>Cargo/</c>).
///         </item>
///         <item>
///             <c>CapturedCaptain</c>, <c>ShipRune</c>/<c>MarkRunes</c>/<c>TryMarkRune</c>,
///             <c>CheckForBoat</c>, <c>AutoAddCannons</c>: пленных капитанов, рун корабля и
///             автоматической установки пушек в этом движке нет.
///         </item>
///     </list>
///
///     Груз (этап 4) живёт в partial-части <c>Cargo/GalleonCargo.cs</c>: настоящий трюм
///     (<c>GalleonHold</c>, поле 9 этого файла), его стенки (<c>HoldItem</c>, список 10),
///     <c>HoldIDs</c>/<c>HoldItemIDs</c>, погрузка брошенного на палубу предмета, выгрузка в банк при
///     сухом доке и выгрузка в грузовой ящик.
/// </summary>
///     Орудийная палуба (этап 3) живёт в partial-части <c>GalleonCannons.cs</c>: список пушек и
///     орудийных пятаков (<c>Cannons</c>/<c>WeaponPads</c>, поля 7–8 этого файла), <c>MaxCannons</c>,
///     <c>CannonDamageMod</c>, <c>CannonTileIDs</c>, постановка/снятие пушки, <c>UpdateCannonID(s)</c>,
///     <c>GetCannonPosition</c> и <c>ShipPosition</c>. Сами пушки и боеприпасы — в
///     <c>Engines/HighSeas/Cannons/</c>.
/// </summary>
using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Items;
using Server.Multis;
using CalcMoves = Server.Movement.Movement;

namespace Server.Engines.HighSeas;

[SerializationGenerator(0, false)]
public abstract partial class BaseGalleon : BaseBoat
{
    /// <summary>Сколько слоёв краски держится до полного выцветания.</summary>
    private const int MaxPaintCoats = 4;

    private static readonly TimeSpan PaintDecayPeriod = TimeSpan.FromDays(14);

    [SerializableField(0, setter: "private")]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private GalleonPilot _galleonPilot;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _paintCoats;

    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _basePaintHue;

    [SerializableField(3, fieldChanged: nameof(OnBaseBoatHueChanged))]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _baseBoatHue;

    [SerializableField(4)]
    [AnchoredDateTime]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private DateTime _nextPaintDecay;

    // Прочность корпуса и срок аварийных заделок (этап 2). Логика — в GalleonDamage.cs: генератор
    // сериализации читает [SerializableField] только из файла с [SerializationGenerator].
    [SerializableField(5, allowFieldChange: nameof(AllowHitsChange), fieldChanged: nameof(OnHitsChanged))]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _hits;

    [SerializableField(6)]
    [AnchoredDateTime]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private DateTime _emergencyRepairEnd;

    // Орудийная палуба (этап 3): пушки и их площадки-пятаки. Поля объявлены здесь, потому что
    // генератор сериализации читает [SerializableField] только из файла с [SerializationGenerator];
    // логика — в GalleonCannons.cs.
    [Tidy]
    [SerializableField(7)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private List<Item> _cannons = [];

    [Tidy]
    [SerializableField(8)]
    private List<Item> _weaponPads = [];

    // Груз (этап 4): настоящий трюм и его палубные стенки. Поля объявлены здесь по той же причине,
    // что и орудийные: генератор сериализации читает [SerializableField] только из файла с
    // [SerializationGenerator]; логика — в Cargo/GalleonCargo.cs.
    [SerializableField(9, setter: "private")]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private GalleonHold _galleonHold;

    [Tidy]
    [SerializableField(10)]
    private List<Item> _holdFixtures = [];

    protected BaseGalleon(Direction facing)
    {
        // Первым делом — целый корпус: от Hits зависят DamageTaken/DamageValue и ID мульти, а
        // BaseBoat в своём конструкторе уже успел выставить ItemID по NorthID (с нулевыми Hits это
        // был бы арт самой тяжёлой ступени повреждений).
        Hits = MaxHits;

        if (facing != Facing)
        {
            Facing = facing;
        }

        // Штатный TillerMan остаётся в мире (на нём держатся речевые команды BaseBoat и табличка
        // состояния), но у галеры рулевой — NPC-кормчий, поэтому рычаг прячем: иначе на палубе два
        // «рулевых», а его двойной клик уводит в BaseBoat.BeginDryDock, который считает любого моба на
        // борту помехой и отказывает в сухом доке.
        if (TillerMan != null)
        {
            TillerMan.Visible = false;
        }

        // BaseBoat в своём конструкторе ставит планки по смещениям мульти 0x0 (ItemID ещё не задан),
        // поэтому пересчитываем компоненты уже для настоящего корпуса.
        UpdateComponents();

        // Орудийные пятаки — из маркеров палубы того же мульти (в ServUO — AddFixtures).
        CreateWeaponPads();

        // Настоящий трюм и его стенки — тоже из маркеров палубы (в ServUO — AddFixtures).
        CreateHoldFixtures();

        // Штатный трюм BaseBoat у галеры заменён настоящим (GalleonHold, этап 4): прячем его, чтобы на
        // палубе не открывались два разных «трюма». Содержимое старого трюма переносится в новый при
        // загрузке (Cargo/GalleonCargo.cs — EnsureHold).
        if (Hold != null)
        {
            Hold.Visible = false;
        }

        AddGalleonPilot(facing);
    }

    /// <summary>Высота палубы над уровнем воды (в ServUO — <c>ZSurface</c>).</summary>
    public virtual int ZSurface => 0;

    /// <summary>
    ///     Сходни ставим по краям корпуса. В ServUO у галер для посадки были ещё швартовые концы
    ///     (<c>MooringLine</c>), а сами планки оставались в центре палубы; здесь швартовых нет, поэтому
    ///     ширину берём из мульти северного направления: она задана в системе координат «на север», а
    ///     поворот для остальных направлений <c>BaseBoat</c> применяет сам.
    /// </summary>
    public override Point2D PortOffset => new(GetHullComponents().Min.X, 0);

    public override Point2D StarboardOffset => new(GetHullComponents().Max.X, 0);

    public override Point3D MarkOffset => new(0, 0, ZSurface);

    private MultiComponentList GetHullComponents() => MultiData.GetComponents(NorthID);

    /// <summary>Список доступа корабля (кто капитан, кто матрос, кому отказано).</summary>
    public ShipAccessList AccessList => HighSeasShips.GetAccessList(this);

    public bool HasPaint => _basePaintHue != 0;

    public static BaseGalleon FindGalleonAt(IPoint2D pnt, Map map) =>
        map == null ? null : FindBoatAt(new Point3D(pnt.X, pnt.Y, 0), map) as BaseGalleon;

    private void AddGalleonPilot(Direction facing)
    {
        var pilot = new GalleonPilot(this)
        {
            Direction = facing
        };

        GalleonPilot = pilot;
        pilot.MoveToWorld(GetPilotLocation(), Map);
    }

    /// <summary>Место кормчего — там же, где движок ставит <c>TillerMan</c>, но на высоте палубы.</summary>
    public Point3D GetPilotLocation()
    {
        var xOffset = 0;
        var yOffset = 0;
        CalcMoves.Offset(Facing, ref xOffset, ref yOffset);

        return new Point3D(
            X + xOffset * TillerManDistance,
            Y + yOffset * TillerManDistance,
            Z + ZSurface
        );
    }

    public override void OnMapChange()
    {
        base.OnMapChange();

        if (Map == null || _galleonPilot?.Deleted != false)
        {
            return;
        }

        // Абсолютная постановка: сюда же приходит перенос корабля из Map.Internal в мир.
        _galleonPilot.MoveToWorld(GetPilotLocation(), Map);
        _galleonPilot.Direction = Facing;

        // Пушки и пятаки ездят вместе с кораблём, но при смене карты их надо поставить абсолютно.
        UpdateCannonPositions();

        // Трюм и его стенки — тоже абсолютно.
        UpdateHoldFixturePositions();
    }

    public override void OnAfterDelete()
    {
        _scuttleToken.Cancel();
        _emergencyRepairToken.Cancel();

        DeleteCannons();
        DeleteFixtures();

        var pilot = _galleonPilot;
        _galleonPilot = null;
        pilot?.Delete();

        HighSeasShips.RemoveAccessList(this);

        base.OnAfterDelete();
    }

    /// <summary>
    ///     Сухой док с учётом кормчего и пушек. <c>BaseBoat.CheckDryDock</c> в этом движке считает
    ///     помехой любого моба на палубе (в ServUO проверялись только игроки с <c>AccessLevel.Player</c>)
    ///     и любой видимый предмет на ней, поэтому на время синхронной проверки кормчий убирается
    ///     с карты, а пушки и пятаки прячутся и сразу возвращаются.
    ///     Штатный <c>BeginDryDock</c> не виртуален — метод перекрыт (<c>new</c>), его вызывают кормчий
    ///     и <see cref="ShipDryDockGump" />.
    /// </summary>
    public new void BeginDryDock(Mobile from)
    {
        // В ServUO это был DryDockResult.Damaged; в перечислении этого движка такого значения нет,
        // поэтому отказ выдаётся здесь, до проверки палубы.
        if (DamageTaken != DamageLevel.Pristine)
        {
            from?.SendLocalizedMessage(1116324); // The ship must be fully repaired before it can be docked!
            return;
        }

        // В ServUO это DryDockResult.Cannon; подходящего значения в перечислении тоже нет.
        if (HasLoadedCannons())
        {
            from?.SendLocalizedMessage(1116323); // You cannot dock the ship with loaded weapons on deck!
            return;
        }

        var pilot = HidePilot();
        SetCannonDeckVisible(false);

        var result = CheckDryDock(from);

        SetCannonDeckVisible(true);
        RestorePilot(pilot);

        if (result != DryDockResult.Valid)
        {
            base.BeginDryDock(from);
            return;
        }

        ShipDryDockGump.DisplayTo(from, this);
    }

    /// <summary>
    ///     Подтверждённый сухой док: проверку, выдачу дея и удаление корабля делает <c>BaseBoat</c>.
    ///     В ServUO корабль уезжал в <c>Map.Internal</c> вместе с пушками; здесь он удаляется, поэтому
    ///     пушки возвращаются владельцу деями.
    /// </summary>
    public void CompleteDryDock(Mobile from)
    {
        if (DamageTaken != DamageLevel.Pristine)
        {
            from?.SendLocalizedMessage(1116324);
            return;
        }

        if (HasLoadedCannons())
        {
            from?.SendLocalizedMessage(1116323);
            return;
        }

        // Деи готовим заранее: после удаления корабля пушек уже не будет.
        var deeds = TakeCannonDeeds();

        var pilot = HidePilot();
        SetCannonDeckVisible(false);

        // Проверку повторяем до выгрузки груза: в этом движке BaseBoat.EndDryDock удаляет корабль
        // вместе с трюмом, поэтому груз надо переправить владельцу заранее — но только если док
        // действительно состоится.
        if (CheckDryDock(from) != DryDockResult.Valid)
        {
            // Штатный код озвучит причину отказа; кормчий при этом ещё спрятан.
            base.EndDryDock(from);

            SetCannonDeckVisible(true);
            RestorePilot(pilot);

            for (var i = 0; i < deeds.Count; i++)
            {
                deeds[i].Delete();
            }

            return;
        }

        UnloadCargoForDryDock(from);

        base.EndDryDock(from);

        if (!Deleted)
        {
            SetCannonDeckVisible(true);
            RestorePilot(pilot);

            for (var i = 0; i < deeds.Count; i++)
            {
                deeds[i].Delete();
            }

            return;
        }

        ReturnDeeds(from, deeds);
    }

    /// <summary>Отдаёт деи пушек владельцу (рюкзак, иначе — под ноги).</summary>
    private static void ReturnDeeds(Mobile from, List<ShipCannonDeed> deeds)
    {
        if (from == null)
        {
            return;
        }

        for (var i = 0; i < deeds.Count; i++)
        {
            if (from.Backpack?.TryDropItem(from, deeds[i], false) != true)
            {
                deeds[i].MoveToWorld(from.Location, from.Map);
            }
        }
    }

    private (GalleonPilot Pilot, Point3D Location, Map Map) HidePilot()
    {
        var pilot = _galleonPilot;

        if (pilot?.Deleted != false)
        {
            return (null, Point3D.Zero, null);
        }

        var state = (pilot, pilot.Location, pilot.Map);

        pilot.MoveToWorld(pilot.Location, Map.Internal);

        return state;
    }

    private void RestorePilot((GalleonPilot Pilot, Point3D Location, Map Map) state)
    {
        if (state.Pilot?.Deleted == false)
        {
            state.Pilot.MoveToWorld(state.Location, state.Map ?? Map);
        }
    }

    public override void OnSpeech(SpeechEventArgs e)
    {
        var from = e.Mobile;

        if (from != null && Contains(from) && HasBoatCommand(e) && !HasCommandRights(from))
        {
            from.SendMessage(0x22, "Вы не вправе командовать этим кораблём.");
            return;
        }

        base.OnSpeech(e);

        // BaseBoat.OnSpeech двигает корабль напрямую: StartMove там не виртуален, поэтому замедление
        // повреждённого корпуса из TrySail на этом пути теряется. Кормчий и корабль слышат один и тот
        // же приказ, и кто из них отработает последним — зависит от сортировки по расстоянию, так что
        // состояние корпуса применяем к ходу ещё раз здесь, уже после штатного обработчика.
        AdjustMoveForDamage(e);
    }

    /// <summary>
    ///     Приводит ход, начатый штатным <c>BaseBoat.OnSpeech</c>, в соответствие с состоянием корпуса:
    ///     пробитый корпус останавливается, повреждённый перезапускается с медленным интервалом
    ///     (см. <see cref="TrySail" />). Курс по карте (keywords 0x60–0x64) не перехватывается: он идёт
    ///     своим таймером в <c>BaseBoat.StartCourse</c>.
    /// </summary>
    private void AdjustMoveForDamage(SpeechEventArgs e)
    {
        if (Deleted)
        {
            return;
        }

        if (Scuttled)
        {
            if (IsMoving)
            {
                StopMove(false);
            }

            return;
        }

        if (DamageTaken < DamageLevel.Heavily || !IsMoving || Order != BoatOrder.Move)
        {
            return;
        }

        var fast = true;
        var single = false;

        var keywords = e.Keywords;

        if (keywords != null)
        {
            for (var i = 0; i < keywords.Length; i++)
            {
                switch (keywords[i])
                {
                    case >= 0x50 and <= 0x57:
                        fast = false;
                        break;
                    case >= 0x58 and <= 0x5F:
                        single = true;
                        break;
                }
            }
        }

        var moving = Moving;
        var drifting = moving is not (Direction.North or Direction.Up or Direction.Right);
        var interval = GetMovementInterval(fast, drifting, out var clientSpeed);

        StartMove(moving, 1, clientSpeed, interval, single, true);
    }

    public bool IsOwner(Mobile from) => from != null && Owner == from;

    /// <summary>Приказ, который понимает и <c>TillerMan</c>, — keyword-и клиента 0x42–0x6B.</summary>
    private static bool HasBoatCommand(SpeechEventArgs e)
    {
        var keywords = e.Keywords;

        if (keywords == null)
        {
            return false;
        }

        for (var i = 0; i < keywords.Length; i++)
        {
            if (keywords[i] is >= 0x42 and <= 0x6B)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     Уровень доступа игрока. Владелец и персонал с правами GM — всегда капитаны; у корабля без
    ///     владельца (например, поставленного ГМом) командовать может любой, иначе такой корабль было
    ///     бы невозможно сдвинуть с места.
    /// </summary>
    public SecurityLevel GetSecurityLevel(Mobile from)
    {
        if (from == null)
        {
            return SecurityLevel.Denied;
        }

        if (Owner == null || from.AccessLevel > AccessLevel.Player || IsOwner(from))
        {
            return SecurityLevel.Captain;
        }

        return AccessList?.GetEffectiveLevel(from) ?? SecurityLevel.Denied;
    }

    /// <summary>Право отдавать приказы (в ServUO — <c>CanCommand</c>): матрос и выше.</summary>
    public bool HasCommandRights(Mobile from) => GetSecurityLevel(from) >= SecurityLevel.Crewman;

    /// <summary>Право находиться на борту и пользоваться кораблём (в ServUO — <c>HasAccess</c>).</summary>
    public bool HasAccess(Mobile from) => Owner == null || GetSecurityLevel(from) > SecurityLevel.Denied;

    #region Окраска корпуса

    private static bool IsBrightPaint(int hue) => hue is 1900 or 2213;

    private static int GetPaintHue(int baseHue, int coats) =>
        IsBrightPaint(baseHue) ? baseHue + coats : baseHue - coats;

    private void OnBaseBoatHueChanged(int oldValue, int newValue)
    {
        if (_paintCoats == 0)
        {
            Hue = newValue;
        }
    }

    /// <summary>Постоянная краска: сразу становится базовым цветом корпуса.</summary>
    public bool TryPermanentPaintBoat(Mobile from, int hue)
    {
        if (_paintCoats > 0)
        {
            from?.SendLocalizedMessage(1149826); // на корабле уже есть другая краска
            return false;
        }

        BaseBoatHue = hue;

        from?.SendLocalizedMessage(1116775); // постоянная краска нанесена
        return true;
    }

    /// <summary>Очередной слой временной краски (выцветает за <see cref="PaintDecayPeriod" />).</summary>
    public bool TryPaintBoat(Mobile from, int baseHue)
    {
        if (_paintCoats > 0 && _basePaintHue != baseHue)
        {
            from?.SendLocalizedMessage(1149826);
            return false;
        }

        if (_paintCoats >= MaxPaintCoats)
        {
            from?.SendLocalizedMessage(1116774); // максимальная насыщенность
            return false;
        }

        if (_paintCoats == 0)
        {
            BasePaintHue = baseHue;
        }

        PaintCoats = _paintCoats + 1;
        NextPaintDecay = Core.Now + PaintDecayPeriod;
        Hue = GetPaintHue(_basePaintHue, _paintCoats);

        from?.SendLocalizedMessage(1116771); // свежий слой краски
        return true;
    }

    /// <summary>Снимает и временную, и постоянную краску (краскоочиститель).</summary>
    public bool RemovePaint()
    {
        if (_paintCoats == 0 && Hue == _baseBoatHue)
        {
            return false;
        }

        BasePaintHue = 0;
        PaintCoats = 0;
        Hue = _baseBoatHue;

        return true;
    }

    public void CheckPaintDecay()
    {
        if (_paintCoats <= 0 || _nextPaintDecay > Core.Now)
        {
            return;
        }

        if (_paintCoats == 1)
        {
            RemovePaint();
            return;
        }

        PaintCoats = _paintCoats - 1;
        NextPaintDecay = Core.Now + PaintDecayPeriod;
        Hue = GetPaintHue(_basePaintHue, _paintCoats);
    }

    #endregion
}
