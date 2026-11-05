/// <summary>
///     Портировано из ServUO (Scripts/Multis/Boats/BaseBoat.cs — блок High Seas, <c>OnTakenDamage</c>,
///     <c>SendDamagePacket</c> и раздел Repairs; Scripts/Services/Expansions/High Seas/Multis/
///     BaseGalleon.cs — <c>InvalidateGalleon</c>, <c>GetMovementInterval</c>, <c>IsNearLandOrDocks</c>).
///
///     Повреждения корпуса галеры: <c>Hits</c>/<c>MaxHits</c>/<c>Durability</c>, уровни
///     <see cref="DamageLevel" />, аварийный арт мульти (±4 к ID корпуса за уровень, как в ServUO),
///     аварийные заделки и постоянный ремонт, затопление при падении прочности ниже
///     <see cref="ScuttleLevel" />.
///
///     Почему блок живёт здесь, а не в <c>BaseBoat</c>: у классических лодок этого движка
///     повреждений нет вовсе (ни <c>Hits</c>, ни <c>DamageLevel</c>, ни <c>ScuttleLevel</c>), а их
///     поведение должно остаться прежним, поэтому всё добавлено в partial-часть галеры. Сам
///     <c>BaseBoat</c> берётся как есть: ход — <c>StartMove</c>/<c>StartTurn</c>, трюмы — штатный
///     <c>Hold</c> и настоящий грузовой <c>GalleonHold</c> (этап 4), кормчий — единственный видимый
///     рулевой (<see cref="GalleonPilot" />).
///
///     Отличия от исходника:
///     <list type="bullet">
///         <item>
///             <c>DamageTaken</c> не сохраняется: он однозначно считается из <c>Hits</c> (в ServUO
///             писались оба поля и могли разойтись). В сейв идут только <c>Hits</c> и срок
///             аварийных заделок.
///         </item>
///         <item>
///             Аварийные заделки в ServUO жили только в таймере и терялись при рестарте — здесь их
///             срок пишется в сейв (<c>[AnchoredDateTime]</c>): иначе корабль с пробитым корпусом
///             после рестарта мгновенно пошёл бы ко дну.
///         </item>
///         <item>
///             <c>Scuttled</c> в ServUO — только запрет хода: корабль ниже <c>ScuttleLevel</c> не
///             тонет, а беспомощно стоит. По требованию этапа здесь он тонет: ниже порога
///             запускается отсчёт <see cref="ScuttleDelay" />, который отменяют аварийные заделки
///             (ровно для этого они в ServUO и нужны), а по истечении корпус уходит на дно.
///         </item>
///         <item>
///             <c>GetMovementInterval</c>: в этом <c>BaseBoat</c> интервалы хода считаются внутри
///             невиртуального <c>StartMove</c>, перехватить их нельзя, поэтому метод заведён свой и
///             вызывается из <see cref="TrySail" />, которым ходит кормчий.
///         </item>
///         <item>
///             <c>IsNearDocks</c> ищет статики по имени тайла (<c>wooden plank</c>/<c>pier</c>), как
///             в ServUO: отдельного API «морской рынок» в движке нет, а регион для причалов не
///             заводится.
///         </item>
///         <item>
///             Груз при затоплении теряется: и настоящий трюм (<c>Cargo/GalleonHold.cs</c>), и штатный
///             <c>Hold</c> удаляются вместе с кораблём. Механики «вернуть груз владельцу» в ServUO нет
///             (<c>OnSink</c> → <c>Delete</c>), а привязки к маяку (<c>CanLinkToLighthouse</c>)
///             в этом движке тоже нет.
///         </item>
///         <item>
///             Не портировано: <c>CheckForBoat</c>/<c>HasAccess</c> для врагов на тонущем корабле и
///             <c>DryDockResult.Damaged</c> — в этом движке нет ни <c>IsEnemy</c> для чужих лодок,
///             ни подходящего члена перечисления, поэтому отказ в сухом доке для повреждённого
///             корпуса выдаётся сообщением в <see cref="BeginDryDock" />.
///         </item>
///     </list>
/// </summary>
using System;
using ModernUO.Serialization;
using Server.Items;
using Server.Multis;
using Server.Network;

namespace Server.Engines.HighSeas;

/// <summary>Уровень повреждений корпуса (в ServUO — <c>Server.Multis.DamageLevel</c>).</summary>
public enum DamageLevel
{
    Pristine = 0,
    Slightly = 1,
    Moderately = 2,
    Heavily = 3,
    Severely = 4
}

public abstract partial class BaseGalleon
{
    /// <summary>Порог прочности (в процентах), ниже которого корпус считается пробитым.</summary>
    public const double ScuttleLevel = 25.0;

    /// <summary>Сколько корпус держится на воде после падения прочности ниже <see cref="ScuttleLevel" />.</summary>
    public static readonly TimeSpan ScuttleDelay = TimeSpan.FromSeconds(30);

    /// <summary>Сколько ткани нужно на аварийные заделки (ServUO: <c>EmergencyRepairClothCost</c>).</summary>
    public const int EmergencyRepairClothCost = 55;

    /// <summary>Сколько досок нужно на аварийные заделки (ServUO: <c>EmergencyRepairWoodCost</c>).</summary>
    public const int EmergencyRepairWoodCost = 25;

    /// <summary>Сколько держатся аварийные заделки (ServUO: <c>EmergencyRepairSpan</c>).</summary>
    public static readonly TimeSpan EmergencyRepairSpan = TimeSpan.FromMinutes(6);

    /// <summary>Сколько досок нужно на один процент постоянного ремонта (ServUO: <c>WoodPer</c>).</summary>
    private const double WoodPerRepairPercent = 17.0;

    /// <summary>Сколько ткани нужно на один процент постоянного ремонта (ServUO: <c>ClothPer</c>).</summary>
    private const double ClothPerRepairPercent = 17.0;

    // Интервалы повторяют private-константы BaseBoat (там они вычисляются через NewBoatMovement,
    // то есть Core.HS), поэтому здесь те же числа с той же оговоркой.
    private static readonly TimeSpan FastDriftInterval = TimeSpan.FromSeconds(Core.HS ? 0.25 : 0.75);
    private static readonly TimeSpan SlowInterval = TimeSpan.FromSeconds(Core.HS ? 0.5 : 0.75);
    private static readonly TimeSpan SlowDriftInterval = TimeSpan.FromSeconds(Core.HS ? 0.5 : 1.5);

    // Поля Hits и EmergencyRepairEnd объявлены в BaseGalleon.cs: генератор сериализации читает
    // [SerializableField] только из файла с [SerializationGenerator].

    private TimerExecutionToken _emergencyRepairToken;
    private TimerExecutionToken _scuttleToken;

    /// <summary>Прочность целого корпуса; у каждой галеры своя (как <c>MaxHits</c> в ServUO).</summary>
    [CommandProperty(AccessLevel.GameMaster, true)]
    public virtual int MaxHits => 25000;

    /// <summary>Прочность корпуса в процентах (в ServUO — <c>BaseBoat.Durability</c>).</summary>
    [CommandProperty(AccessLevel.GameMaster, true)]
    public double Durability => (double)_hits / MaxHits * 100.0;

    /// <summary>Уровень повреждений; считается из <see cref="Durability" />, а не хранится отдельно.</summary>
    [CommandProperty(AccessLevel.GameMaster, true)]
    public DamageLevel DamageTaken => GetDamageLevel(_hits);

    /// <summary>
    ///     Номер «ступени» аварийного арта: каждый уровень сдвигает ID корпуса на 4. У британской
    ///     галеры ступеней две, у остальных три (см. переопределения в наследниках).
    /// </summary>
    public virtual int DamageValue => DamageTaken switch
    {
        DamageLevel.Pristine or DamageLevel.Slightly   => 0,
        DamageLevel.Moderately or DamageLevel.Heavily  => 1,
        _                                              => 2
    };

    /// <summary>Корпус пробит и не держится на аварийных заделках — корабль тонет.</summary>
    [CommandProperty(AccessLevel.GameMaster, true)]
    public bool Scuttled => !IsUnderEmergencyRepairs() && Durability < ScuttleLevel;

    private bool AllowHitsChange(ref int value)
    {
        value = Math.Clamp(value, 0, MaxHits);
        return true;
    }

    private void OnHitsChanged(int oldValue, int newValue)
    {
        InvalidateGalleon();

        _galleonPilot?.InvalidateProperties();

        if (GetDamageLevel(oldValue) != DamageLevel.Severely && DamageTaken == DamageLevel.Severely)
        {
            _galleonPilot?.Say("Корпус едва держится, капитан!");
        }

        UpdateScuttleState();
    }

    private DamageLevel GetDamageLevel(int hits) => ((double)hits / MaxHits * 100.0) switch
    {
        >= 100.0 => DamageLevel.Pristine,
        >= 75.0  => DamageLevel.Slightly,
        >= 50.0  => DamageLevel.Moderately,
        >= 25.0  => DamageLevel.Heavily,
        _        => DamageLevel.Severely
    };

    /// <summary>
    ///     Переставляет корпус на арт текущего уровня повреждений (в ServUO — <c>InvalidateGalleon</c>).
    ///     Повреждённый мульти имеет другой футпринт, поэтому планки и трюм пересчитываются.
    /// </summary>
    public void InvalidateGalleon()
    {
        ItemID = Facing switch
        {
            Direction.North => NorthID,
            Direction.East  => EastID,
            Direction.South => SouthID,
            Direction.West  => WestID,
            _               => ItemID
        };

        UpdateComponents();
    }

    /// <summary>Корабль получил повреждения (в ServUO — <c>BaseBoat.OnTakenDamage</c>).</summary>
    public virtual void OnTakenDamage(Mobile from, int damage)
    {
        if (Deleted || damage <= 0)
        {
            return;
        }

        Hits = _hits - damage;

        SendDamagePacket(from, damage);

        // Глухой удар в корпус: единственный «эффект попадания», который был и в ServUO
        // (там звук играли сами снаряды, здесь снарядов ещё нет).
        Effects.PlaySound(Location, Map, 0x3B3);
    }

    public virtual void OnTakenDamage(int damage) => OnTakenDamage(null, damage);

    /// <summary>Показывает нападающему всплывающее число урона (пакет 0x0B/0xBF).</summary>
    public virtual void SendDamagePacket(Mobile from, int amount)
    {
        if (amount == 0)
        {
            return;
        }

        var ns = from?.NetState ?? from?.GetDamageMaster(null)?.NetState;

        ns?.SendDamage(Serial, amount);
    }

    #region Аварийные заделки и постоянный ремонт

    /// <summary>Корпус держится на аварийных заделках (в ServUO — <c>IsUnderEmergencyRepairs</c>).</summary>
    public bool IsUnderEmergencyRepairs() => _emergencyRepairEnd > Core.Now;

    /// <summary>Сколько ещё держатся аварийные заделки (в ServUO — <c>GetEndEmergencyRepairs</c>).</summary>
    public TimeSpan GetEndEmergencyRepairs()
    {
        var remaining = _emergencyRepairEnd - Core.Now;

        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }

    /// <summary>
    ///     Аварийные заделки: 25 досок и 55 ткани из рюкзака и трюма дают шесть минут хода даже с
    ///     пробитым корпусом (в ServUO — <c>TryEmergencyRepair</c>). Отменяют начавшееся затопление.
    /// </summary>
    public bool TryEmergencyRepair(Mobile from)
    {
        if (Deleted || from?.Backpack == null)
        {
            return false;
        }

        var pack = from.Backpack;
        Container hold = GalleonHold ?? (Container)Hold;

        if (GetWoodAmount(pack) + GetWoodAmount(hold) < EmergencyRepairWoodCost ||
            GetClothAmount(pack) + GetClothAmount(hold) < EmergencyRepairClothCost)
        {
            from.SendLocalizedMessage(1116591, $"{EmergencyRepairClothCost}\t{EmergencyRepairWoodCost}");
            return false;
        }

        var woodLeft = EmergencyRepairWoodCost - ConsumeWood(pack, EmergencyRepairWoodCost);

        if (woodLeft > 0)
        {
            ConsumeWood(hold, woodLeft);
        }

        var clothLeft = EmergencyRepairClothCost - ConsumeCloth(pack, EmergencyRepairClothCost);

        if (clothLeft > 0)
        {
            ConsumeCloth(hold, clothLeft);
        }

        _emergencyRepairEnd = Core.Now + EmergencyRepairSpan;
        StartEmergencyRepairTimer();

        // Заделки держат воду, поэтому затопление (если оно уже началось) отменяется.
        UpdateScuttleState();

        from.SendLocalizedMessage(1116592, $"{(int)EmergencyRepairSpan.TotalMinutes}");

        return true;
    }

    /// <summary>
    ///     Постоянный ремонт (в ServUO — <c>TryRepairs</c>): доски и ткань из рюкзака и трюма
    ///     восстанавливают часть прочности пропорционально недостающей, с бонусом от Carpentry и
    ///     Tailoring. Проверку «корабль у берега или у причала» делает вызывающий
    ///     (<see cref="GalleonPilot" />), как и в ServUO.
    /// </summary>
    public void TryRepairs(Mobile from)
    {
        if (Deleted || from?.Backpack == null)
        {
            return;
        }

        var pack = from.Backpack;
        Container hold = GalleonHold ?? (Container)Hold;

        var missingPercent = 100.0 - Durability;

        var woodNeeded = WoodPerRepairPercent * missingPercent;
        var clothNeeded = ClothPerRepairPercent * missingPercent;

        woodNeeded -= from.Skills.Carpentry.Value / 200.0 * woodNeeded;
        clothNeeded -= from.Skills.Tailoring.Value / 200.0 * clothNeeded;

        var wood = GetWoodAmount(pack) + GetWoodAmount(hold);
        var cloth = GetClothAmount(pack) + GetClothAmount(hold);

        var minWood = woodNeeded / 10.0;
        var minCloth = clothNeeded / 10.0;

        if (wood < minWood || cloth < minCloth)
        {
            from.SendLocalizedMessage(1116593, $"{(int)minCloth}\t{(int)minWood}");
            return;
        }

        double woodUsed;
        double clothUsed;
        double woodPercent;
        double clothPercent;

        if (wood >= woodNeeded)
        {
            woodUsed = woodNeeded;
            woodPercent = 100.0;
        }
        else
        {
            woodUsed = wood;
            woodPercent = wood / woodNeeded * 100.0;
        }

        if (cloth >= clothNeeded)
        {
            clothUsed = clothNeeded;
            clothPercent = 100.0;
        }
        else
        {
            clothUsed = cloth;
            clothPercent = cloth / clothNeeded * 100.0;
        }

        // Ремонт идёт по тому материалу, которого хватает меньше: излишек второго не тратится.
        if (clothUsed > woodUsed)
        {
            clothUsed = woodUsed;
            clothPercent = woodPercent;
        }
        else if (woodUsed > clothUsed)
        {
            woodUsed = clothUsed;
            woodPercent = clothPercent;
        }

        var totalPercent = Math.Min(100.0, (woodPercent + clothPercent) / 2.0);

        var woodToConsume = (int)woodUsed;
        var clothToConsume = (int)clothUsed;

        var woodRemaining = woodToConsume - ConsumeWood(pack, woodToConsume);

        if (woodRemaining > 0)
        {
            ConsumeWood(hold, woodRemaining);
        }

        var clothRemaining = clothToConsume - ConsumeCloth(pack, clothToConsume);

        if (clothRemaining > 0)
        {
            ConsumeCloth(hold, clothRemaining);
        }

        Hits = _hits + (int)((MaxHits - _hits) * (totalPercent / 100.0));

        // Полный ремонт снимает и аварийные заделки: корпус больше не нуждается в них.
        StopEmergencyRepairs();

        UpdateScuttleState();

        from.SendLocalizedMessage(1116598, $"{clothToConsume}\t{woodToConsume}\t{(int)Durability}");
    }

    private void StartEmergencyRepairTimer()
    {
        _emergencyRepairToken.Cancel();

        var remaining = GetEndEmergencyRepairs();

        if (remaining <= TimeSpan.Zero)
        {
            _emergencyRepairEnd = default;
            return;
        }

        Timer.StartTimer(remaining, EndEmergencyRepairs, out _emergencyRepairToken);
    }

    private void EndEmergencyRepairs()
    {
        if (Deleted)
        {
            return;
        }

        _emergencyRepairEnd = default;

        MessageCrew("Аварийные заделки разошлись — корпус снова течёт!");

        UpdateScuttleState();
    }

    private void StopEmergencyRepairs()
    {
        _emergencyRepairEnd = default;
        _emergencyRepairToken.Cancel();
    }

    /// <summary>
    ///     Восстанавливает состояние после загрузки: таймер заделок не сохраняется, поэтому его нужно
    ///     завести заново, а корпус, загруженный пробитым и без заделок, снова начинает тонуть.
    ///     Здесь же достраиваются орудийные пятаки кораблей, сохранённых до этапа 3.
    /// </summary>
    [AfterDeserialization(false)]
    private void AfterDeserialization()
    {
        if (_hits <= 0)
        {
            // Корабль с нулевой прочностью не сохраняется (он уже утонул), значит запись битая.
            Hits = MaxHits;
        }

        EnsureWeaponPads();
        UpdateCannonPositions();

        // Корабли, сохранённые до этапа 4, пришли без трюма — достраиваем его и ставим на место.
        EnsureHold();
        UpdateHoldFixturePositions();

        if (IsUnderEmergencyRepairs())
        {
            StartEmergencyRepairTimer();
            return;
        }

        _emergencyRepairEnd = default;

        UpdateScuttleState();
    }

    private static int GetWoodAmount(Container container) =>
        container == null ? 0 : container.GetAmount(typeof(Board)) + container.GetAmount(typeof(Log));

    private static int GetClothAmount(Container container) =>
        container == null ? 0 : container.GetAmount(typeof(Cloth)) + container.GetAmount(typeof(UncutCloth));

    private static int ConsumeWood(Container container, int amount)
    {
        if (container == null || amount <= 0)
        {
            return 0;
        }

        var consumed = container.ConsumeUpTo(typeof(Board), amount);

        return consumed < amount ? consumed + container.ConsumeUpTo(typeof(Log), amount - consumed) : consumed;
    }

    private static int ConsumeCloth(Container container, int amount)
    {
        if (container == null || amount <= 0)
        {
            return 0;
        }

        var consumed = container.ConsumeUpTo(typeof(Cloth), amount);

        return consumed < amount ? consumed + container.ConsumeUpTo(typeof(UncutCloth), amount - consumed) : consumed;
    }

    #endregion

    #region Затопление

    /// <summary>
    ///     Держит состояние затопления в согласии с прочностью: пробитый корпус начинает тонуть,
    ///     заделки (или ремонт) отменяют отсчёт.
    /// </summary>
    private void UpdateScuttleState()
    {
        if (Deleted)
        {
            return;
        }

        if (!Scuttled)
        {
            _scuttleToken.Cancel();
            return;
        }

        if (_scuttleToken.Running)
        {
            return;
        }

        MessageCrew("Корпус не держит! Корабль идёт ко дну — нужны аварийные заделки!");
        _galleonPilot?.Say("Мы тонем, капитан!");

        Timer.StartTimer(ScuttleDelay, Sink, out _scuttleToken);
    }

    /// <summary>
    ///     Затапливает корабль: корпус, кормчий и запись в <see cref="HighSeasShips" /> удаляются,
    ///     груз трюма уходит на дно вместе с кораблём (в ServUO — <c>OnSink</c> → <c>Delete</c>).
    /// </summary>
    public void Sink()
    {
        if (Deleted)
        {
            return;
        }

        _scuttleToken.Cancel();
        _emergencyRepairToken.Cancel();

        MessageCrew("Корабль утонул. Всё, что было в трюме, потеряно.");
        _galleonPilot?.Say("Корабль идёт ко дну!");

        Effects.SendLocationEffect(Location, Map, 0x352D, 16, 4);
        Effects.PlaySound(Location, Map, 0x364);

        Delete();
    }

    /// <summary>Сообщение всем, кто на борту (в ServUO — <c>SendMessageToAllOnBoard</c>).</summary>
    private void MessageCrew(string text)
    {
        var map = Map;

        if (map == null || map == Map.Internal)
        {
            return;
        }

        foreach (var m in map.GetMobilesInRange(Location, 18))
        {
            if (Contains(m))
            {
                m.SendMessage(0x22, text);
            }
        }
    }

    #endregion

    #region Ход повреждённого корпуса

    /// <summary>
    ///     Интервал хода с учётом повреждений (в ServUO — <c>BaseBoat.GetMovementInterval</c>).
    /// </summary>
    /// <remarks>
    ///     ServUO отдавал повреждённому корпусу дрейфовые интервалы: «быстрый» ход становился
    ///     <c>FastDriftInterval</c> (500 мс), «медленный» — <c>SlowDriftInterval</c> (1000 мс), то есть
    ///     вдвое медленнее целого. В этом движке после High Seas-переделки дрейфовые интервалы
    ///     совпали с обычными (250/500 мс), поэтому портирован смысл, а не числа: повреждённый
    ///     корпус идёт не быстрее самого медленного хода движка и с пониженной клиентской скоростью
    ///     (0x3/0x2 вместо 0x4/0x3). Иначе «медленнее» не было бы видно вовсе.
    /// </remarks>
    public TimeSpan GetMovementInterval(bool fast, bool drifting, out int clientSpeed)
    {
        if (DamageTaken < DamageLevel.Heavily)
        {
            clientSpeed = fast ? 0x4 : 0x3;

            return (fast, drifting) switch
            {
                (true, _)     => FastDriftInterval,
                (false, true) => SlowDriftInterval,
                _             => SlowInterval
            };
        }

        clientSpeed = fast ? 0x3 : 0x2;

        return SlowDriftInterval;
    }

    /// <summary>
    ///     Ход галеры: целый корпус идёт штатным <c>BaseBoat.StartMove(dir, fast)</c>, повреждённый —
    ///     тем же движком, но с интервалом из <see cref="GetMovementInterval" />. Пробитый и уже
    ///     тонущий корабль не слушается вовсе.
    /// </summary>
    public bool TrySail(Direction dir, bool fast)
    {
        if (Deleted || Scuttled)
        {
            return false;
        }

        if (DamageTaken < DamageLevel.Heavily)
        {
            return StartMove(dir, fast);
        }

        var drifting = dir is not (Direction.North or Direction.Up or Direction.Right);
        var interval = GetMovementInterval(fast, drifting, out var clientSpeed);

        return StartMove(dir, 1, clientSpeed, interval, false, true);
    }

    #endregion

    #region Берег и причалы

    /// <summary>Корабль стоит у берега или у причала — там можно ставить пушки и чинить корпус.</summary>
    public static bool IsNearLandOrDocks(BaseBoat boat) => IsNearLand(boat) || IsNearDocks(boat);

    /// <summary>Ищет проходимую сушу в радиусе <paramref name="range" /> клеток (в ServUO — <c>IsNearLand</c>).</summary>
    public static bool IsNearLand(BaseBoat boat, int range = 12)
    {
        var map = boat?.Map;

        if (map == null || map == Map.Internal)
        {
            return false;
        }

        for (var x = boat.X - range; x <= boat.X + range; x++)
        {
            for (var y = boat.Y - range; y <= boat.Y + range; y++)
            {
                var land = map.Tiles.GetLandTile(x, y);

                if ((TileData.LandTable[land.ID & TileData.MaxLandValue].Flags & TileFlag.Impassable) == 0)
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>Ищет причал в радиусе <paramref name="range" /> клеток (в ServUO — <c>IsNearDocks</c>).</summary>
    public static bool IsNearDocks(BaseBoat boat, int range = 12)
    {
        var map = boat?.Map;

        if (map == null || map == Map.Internal)
        {
            return false;
        }

        for (var x = boat.X - range; x <= boat.X + range; x++)
        {
            for (var y = boat.Y - range; y <= boat.Y + range; y++)
            {
                foreach (var tile in map.Tiles.GetStaticTiles(x, y))
                {
                    var name = TileData.ItemTable[tile.ID & TileData.MaxItemValue].Name;

                    if (string.Equals(name, "wooden plank", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(name, "pier", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    #endregion
}
