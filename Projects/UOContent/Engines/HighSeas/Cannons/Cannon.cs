/// <summary>
///     Портировано из ServUO (Scripts/Services/Expansions/High Seas/Items/Cannons and Ammo/Cannon.cs).
///
///     Первое поколение пушек High Seas: обычный предмет на палубе, который заряжается предметами из
///     рюкзака игрока. Полный цикл (как в ServUO):
///     <list type="number">
///         <item>чистка банником (<see cref="Swab" />) — <see cref="TryClean" />;</item>
///         <item>пороховой заряд (<see cref="PowderCharge" />) — <see cref="TryCharge" />;</item>
///         <item>шомпол (<see cref="Ramrod" />) и снаряд (<see cref="ICannonAmmo" />, выбирается
///             прицелом <c>Target</c>) — <see cref="TryLoad" />;</item>
///         <item>запальный шнур (<see cref="FuseCord" />) — <see cref="TryPrime" />;</item>
///         <item>поджог фитиля горящей спичкой/факелом — <see cref="TryLightFuse" />, затем
///             выстрел по направлению ствола через <see cref="Shoot(Mobile)" />;</item>
///         <item>после выстрела ствол грязный — снова чистка банником.</item>
///     </list>
///
///     В ServUO рядом жил второй, «журнальный» вариант пушки — <c>BaseShipCannon</c> (см.
///     <c>ShipCannon.cs</c>), где порох, снаряд и фитиль лежат в самой пушке, а весь цикл запускается
///     одной кнопкой. Он портирован отдельно: у него своё окно и своё состояние.
///
///     Адаптация под этот движок:
///     <list type="bullet">
///         <item>
///             <c>DamageState</c> не хранится — считается из <see cref="Hits" /> (как уровень
///             повреждений корпуса на этапе 2).
///         </item>
///         <item>
///             Положение пушки на палубе хранится смещением в «северной» системе координат корабля
///             (<see cref="PadOffset" />), а не готовыми координатами: тогда и поворот корабля, и
///             смена арта повреждённого корпуса пересчитываются одной формулой
///             (<c>BaseGalleon.GetPadLocation</c>), а не поиском по тайлам палубы.
///         </item>
///         <item>
///             Урон по кораблю-цели идёт через этап 2 (<see cref="BaseGalleon.OnTakenDamage" />),
///             урон по мобам — через <c>AOS.Damage</c>; стихии берутся из <see cref="AmmoInfo" />.
///             Выстрел разрешается сразу по всей линии полёта, а не отложенными таймерами на каждую
///             клетку (в ServUO снаряд «летел» по 0.1 с на клетку): на однопоточном цикле разница
///             только косметическая, зато результат детерминирован и проверяем в тестах.
///         </item>
///         <item>
///             <c>IsEnemy</c> у кораблей в этом движке нет, поэтому целью выстрела становится любой
///             чужой корабль в линии огня (свой — никогда).
///         </item>
///         <item>
///             Все отложенные шаги цикла идут через <c>Timer.StartTimer</c> с токенами, которые
///             отменяются в <see cref="OnDelete" />.
///         </item>
///         <item>
///             Целиться можно прицелом (<see cref="TryFireAt" /> — <c>Target</c> по кораблю, мобу или
///             точке) или просто поджечь фитиль и стрелять по направлению ствола.
///         </item>
///     </list>
/// </summary>
using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Collections;
using Server.ContextMenus;
using Server.Items;
using Server.Mobiles;
using Server.Multis;
using Server.Regions;
using Server.Targeting;

namespace Server.Engines.HighSeas;

/// <summary>Место пушки на палубе (в ServUO — <c>ShipPosition</c>; порядок важен для gump-заголовков).</summary>
public enum ShipPosition
{
    Bow,
    BowPort,
    BowStarboard,
    AmidShipPort,
    AmidShipStarboard,
    AftPort,
    AftStarboard,
    Aft
}

/// <summary>Пушка на палубе галеры.</summary>
public interface IShipCannon : IEntity
{
    int Hits { get; set; }

    /// <summary>Дальность выстрела в клетках.</summary>
    int Range { get; }

    /// <summary>Чем заряжен ствол.</summary>
    AmmunitionType AmmoType { get; }

    /// <summary>Корабль, на котором стоит пушка.</summary>
    BaseGalleon Galleon { get; }

    /// <summary>Место на палубе (для окна и подсказок).</summary>
    ShipPosition Position { get; set; }

    /// <summary>Куда смотрит ствол.</summary>
    Direction Facing { get; }

    /// <summary>Смещение от центра корабля в «северной» системе координат.</summary>
    Point3D PadOffset { get; set; }

    /// <summary>Состояние ствола.</summary>
    DamageLevel DamageState { get; }

    /// <summary>Класс пушки: от него зависит арт на палубе и дей при разборке.</summary>
    CannonPower Power { get; }

    /// <summary>Дей или предмет для разборки пушки.</summary>
    ShipCannonDeed GetDeed { get; }

    /// <summary>Ствол готов к выстрелу: чист, заряжен, снаряжён и взведён.</summary>
    bool CanLight { get; }

    void OnDamage(int damage, Mobile shooter);

    void LightFuse(Mobile from);
}

/// <summary>Общая для обоих поколений пушек часть выстрела.</summary>
internal static class CannonShot
{
    /// <summary>Сколько дробин даёт картечь по мобам на палубе цели (в ServUO — 15).</summary>
    private const int GrapeshotPellets = 15;

    /// <summary>Урон одной дроби картечи по мобам (в ServUO — 35, независимо от <see cref="AmmoInfo" />).</summary>
    private const int GrapeshotPelletDamage = 35;

    /// <summary>Вспышка и звук выстрела у дула.</summary>
    public static void MuzzleEffects(IShipCannon cannon)
    {
        var p = cannon.Location;
        var map = cannon.Map;

        p.Z -= 3;

        switch (cannon.Facing)
        {
            case Direction.North:
                p.Y--;
                break;
            case Direction.East:
                p.X++;
                break;
            case Direction.South:
                p.Y++;
                break;
            case Direction.West:
                p.X--;
                break;
        }

        Effects.SendLocationEffect(p, map, 0x3728, 15, 10);
        Effects.PlaySound(p, map, 0x664);
    }

    /// <summary>
    ///     Попадание в корабль. Картечь бьёт по корпусу пятнадцатью дробями и задевает всех, кто на
    ///     палубе; ядро — одним ударом (в ServUO ровно так же). Заодно достаётся чужим пушкам рядом
    ///     с точкой попадания.
    /// </summary>
    public static void HitShip(IShipCannon cannon, BaseGalleon target, Point3D point, Mobile shooter)
    {
        var galleon = cannon.Galleon;
        var ammo = AmmoInfo.GetAmmoInfo(cannon.AmmoType);

        if (ammo == null || target?.Deleted != false)
        {
            return;
        }

        var damage = (int)(ammo.GetDamage() * (galleon?.CannonDamageMod ?? 1.0));

        // Точка попадания со смещением — как в ServUO: снаряд не обязан прийти в центр палубы.
        var z = target.ZSurface;
        var hit = ammo.AmmoType == AmmunitionType.Grapeshot ? point : GetImpactPoint(cannon, point, z);

        if (cannon.AmmoType == AmmunitionType.Grapeshot)
        {
            for (var i = 0; i < GrapeshotPellets; i++)
            {
                var loc = new Point3D(hit.X + Utility.RandomMinMax(0, 4), hit.Y + Utility.RandomMinMax(0, 4), hit.Z);

                Effects.SendLocationEffect(loc, target.Map, 0x36CB, 15, 15);
                target.OnTakenDamage(shooter, damage);

                HitMobilesOnBoard(cannon, shooter, loc, ammo);
            }
        }
        else
        {
            Effects.SendLocationEffect(hit, target.Map, 0x36CB, 15, 15);
            Effects.PlaySound(hit, target.Map, 0x207);
            target.OnTakenDamage(shooter, damage);
            HitItems(cannon, hit, shooter, damage);
        }

        HarmCrew(cannon, target, shooter, hit);
    }

    /// <summary>Урон по мобам в клетке попадания (ядро и картечь).</summary>
    public static void HitMobiles(IShipCannon cannon, List<Mobile> mobs, Point3D point, Mobile shooter)
    {
        var ammo = AmmoInfo.GetAmmoInfo(cannon.AmmoType);

        if (ammo == null || shooter == null)
        {
            return;
        }

        var galleon = cannon.Galleon;
        var damage = (int)(ammo.GetDamage() * (galleon?.CannonDamageMod ?? 1.0));

        for (var i = 0; i < mobs.Count; i++)
        {
            var mob = mobs[i];

            if (mob?.Deleted != false)
            {
                continue;
            }

            shooter.DoHarmful(mob);
            AOS.Damage(
                mob,
                shooter,
                damage,
                ammo.PhysicalDamage,
                ammo.FireDamage,
                ammo.ColdDamage,
                ammo.PoisonDamage,
                ammo.EnergyDamage
            );

            Effects.SendLocationEffect(mob.Location, mob.Map, Utility.RandomBool() ? 14000 : 14013, 15, 10);
            Effects.PlaySound(mob.Location, mob.Map, 0x207);
        }
    }

    /// <summary>Чужие пушки рядом с точкой попадания получают тот же урон.</summary>
    public static void HitItems(IShipCannon cannon, Point3D point, Mobile shooter, int damage)
    {
        var map = cannon.Map;

        if (map == null || map == Map.Internal)
        {
            return;
        }

        foreach (var item in map.GetItemsInRange<Item>(point, 1))
        {
            if (item is IShipCannon other && !ReferenceEquals(other, cannon) &&
                cannon.Galleon?.Contains(item) != true)
            {
                other.OnDamage(damage, shooter);
            }
        }
    }

    /// <summary>
    ///     Кто стоит у точки попадания и может пострадать: игроки, дикие и приручённые существа.
    ///     Экипаж стреляющего корабля не задевается.
    /// </summary>
    public static List<Mobile> CollectMobiles(IShipCannon cannon, Mobile shooter, Point3D point, Map map)
    {
        var list = new List<Mobile>();

        if (map == null || map == Map.Internal || shooter == null)
        {
            return list;
        }

        foreach (var mob in map.GetMobilesInRange<Mobile>(point, 0))
        {
            if (mob.Deleted || mob == shooter || !shooter.CanBeHarmful(mob, false) ||
                cannon.Galleon?.Contains(mob) == true)
            {
                continue;
            }

            if (mob is PlayerMobile || mob is BaseCreature)
            {
                list.Add(mob);
            }
        }

        return list;
    }

    /// <summary>Чужой корабль в точке выстрела.</summary>
    public static BaseGalleon FindShip(IShipCannon cannon, Point3D point, Map map) =>
        BaseGalleon.FindGalleonAt(point, map) is { } target && !ReferenceEquals(target, cannon.Galleon) ? target : null;

    /// <summary>
    ///     Пиратский маяк Rising Tide в клетке выстрела. Маяк — предмет, а не мобил, поэтому клиент его
    ///     не атакует: единственный путь урона с корабля — эта проверка линии огня
    ///     (см. <see cref="PlunderBeacon.ApplyDamage" />).
    /// </summary>
    public static bool TryHitBeacon(IShipCannon cannon, Mobile shooter, Point3D point, Map map)
    {
        if (map == null || map == Map.Internal)
        {
            return false;
        }

        foreach (var beacon in map.GetItemsInRange<PlunderBeacon>(point, 1))
        {
            if (beacon.Deleted || !beacon.CanDamage)
            {
                continue;
            }

            beacon.ApplyDamage(shooter, AmmoInfo.GetAmmoInfo(cannon.AmmoType)?.GetDamage() ?? 0);

            return true;
        }

        return false;
    }

    /// <summary>
    ///     Ствол бьёт только в свою сторону: цель должна лежать в секторе <see cref="IShipCannon.Facing" />
    ///     (допускаются два соседних диагональных луча). Направление выстрела — всегда по стволу.
    /// </summary>
    public static bool TryGetFireDirection(IShipCannon cannon, Point3D point, out Direction direction)
    {
        direction = cannon.Facing;

        var aim = Utility.GetDirection(cannon.Location, point);
        var delta = ((int)aim - (int)direction + 8) % 8;

        return delta is 0 or 1 or 7;
    }

    /// <summary>Цель в пределах дальности ствола.</summary>
    public static bool IsInRange(IShipCannon cannon, Point3D point) =>
        Utility.InRange(cannon.Location, point, Math.Max(1, cannon.Range));

    /// <summary>Точка попадания ядра: со смещением в сторону стрелка, как в ServUO.</summary>
    private static Point3D GetImpactPoint(IShipCannon cannon, Point3D point, int z) =>
        Utility.GetDirection(cannon.Location, point) switch
        {
            Direction.North => new Point3D(point.X + Utility.RandomMinMax(-1, 1), point.Y + Utility.RandomMinMax(-2, 0), z),
            Direction.South => new Point3D(point.X + Utility.RandomMinMax(-1, 1), point.Y + Utility.RandomMinMax(0, 2), z),
            Direction.East  => new Point3D(point.X + Utility.RandomMinMax(0, 2), point.Y + Utility.RandomMinMax(-1, 1), z),
            _               => new Point3D(point.X + Utility.RandomMinMax(-2, 0), point.Y + Utility.RandomMinMax(-1, 1), z)
        };

    /// <summary>Картечь задевает всех, кто стоит у точки попадания на палубе цели.</summary>
    private static void HitMobilesOnBoard(IShipCannon cannon, Mobile shooter, Point3D point, AmmoInfo ammo)
    {
        var map = cannon.Map;

        if (map == null || map == Map.Internal || shooter == null)
        {
            return;
        }

        foreach (var mob in map.GetMobilesInRange<Mobile>(point, 0))
        {
            if (mob is not PlayerMobile && mob is not BaseCreature)
            {
                continue;
            }

            if (!shooter.CanBeHarmful(mob, false) || cannon.Galleon?.Contains(mob) == true)
            {
                continue;
            }

            shooter.DoHarmful(mob);
            AOS.Damage(
                mob,
                shooter,
                GrapeshotPelletDamage,
                ammo.PhysicalDamage,
                ammo.FireDamage,
                ammo.ColdDamage,
                ammo.PoisonDamage,
                ammo.EnergyDamage
            );
        }
    }

    /// <summary>
    ///     Владелец корабля-цели и его экипаж становятся врагами стрелка. В ServUO сначала помечался
    ///     владелец, иначе — самый старший по доступу игрок на палубе.
    /// </summary>
    private static void HarmCrew(IShipCannon cannon, BaseGalleon target, Mobile shooter, Point3D hit)
    {
        if (shooter?.Deleted != false)
        {
            return;
        }

        var victim = target.Owner;

        if (victim != null && target.Contains(victim) && shooter.CanBeHarmful(victim, false))
        {
            shooter.DoHarmful(victim);
            return;
        }

        var map = target.Map;

        if (map == null || map == Map.Internal)
        {
            return;
        }

        Mobile best = null;
        var bestLevel = SecurityLevel.Passenger;

        foreach (var mob in map.GetMobilesInRange<PlayerMobile>(hit, 8))
        {
            if (mob.Deleted || !target.Contains(mob) || !shooter.CanBeHarmful(mob, false))
            {
                continue;
            }

            var level = target.GetSecurityLevel(mob);

            if (best == null || level > bestLevel)
            {
                best = mob;
                bestLevel = level;
            }
        }

        if (best != null)
        {
            shooter.DoHarmful(best);
        }
        else if (victim != null && shooter.IsHarmfulCriminal(victim))
        {
            shooter.CriminalAction(false);
        }
    }
}

[SerializationGenerator(0, false)]
public abstract partial class BaseCannon : Item, IShipCannon
{
    /// <summary>Задержка между поджогом фитиля и выстрелом (в ServUO — 2 с).</summary>
    public static readonly TimeSpan FuseDelay = TimeSpan.FromSeconds(2);

    // Индексы полей закреплены: 0–8 объявлены здесь, дальше только добавлять.
    [SerializableField(0, allowFieldChange: nameof(AllowHitsChange), fieldChanged: nameof(OnHitsChanged))]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _hits;

    [SerializableField(1, fieldChanged: nameof(OnStateChanged))]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _cleaned;

    [SerializableField(2, fieldChanged: nameof(OnStateChanged))]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _charged;

    [SerializableField(3, fieldChanged: nameof(OnStateChanged))]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _primed;

    [SerializableField(4, setter: "private", fieldChanged: nameof(OnAmmoChanged))]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private AmmunitionType _ammoType;

    [SerializableField(5, setter: "private")]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private BaseGalleon _galleon;

    [SerializableField(6)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private ShipPosition _position;

    [SerializableField(7)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _cleanliness;

    [SerializableField(8, fieldChanged: nameof(OnPadOffsetChanged))]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Point3D _padOffset;

    private readonly List<TimerExecutionToken> _tokens = [];

    protected BaseCannon(BaseGalleon galleon) : base(16918)
    {
        Movable = false;
        _cleaned = true;
        _galleon = galleon;
        _ammoType = AmmunitionType.Empty;
        _hits = MaxHits;
    }

    public virtual int MaxHits => 100;

    public virtual int Range => 8;

    /// <summary>Сколько длится один шаг перезарядки.</summary>
    public virtual TimeSpan ActionTime => TimeSpan.FromSeconds(1.5);

    public virtual ShipCannonDeed GetDeed => null;

    public virtual CannonPower Power => CannonPower.Light;

    [CommandProperty(AccessLevel.GameMaster)]
    public Direction Facing => GetFacing();

    [CommandProperty(AccessLevel.GameMaster)]
    public DamageLevel DamageState => GetDamageLevel();

    /// <summary>Ствол готов к выстрелу.</summary>
    [CommandProperty(AccessLevel.GameMaster)]
    public bool CanLight => _cleaned && _charged && _primed && _ammoType != AmmunitionType.Empty;

    [CommandProperty(AccessLevel.GameMaster)]
    public double Durability => (double)_hits / MaxHits * 100.0;

    public override bool ForceShowProperties => true;

    private bool AllowHitsChange(ref int value)
    {
        value = Math.Clamp(value, 0, MaxHits);
        return true;
    }

    private void OnHitsChanged(int oldValue, int newValue)
    {
        InvalidateProperties();
        CheckDestroyed(null);
    }

    private void OnStateChanged(bool oldValue, bool newValue) => InvalidateProperties();

    private void OnAmmoChanged(AmmunitionType oldValue, AmmunitionType newValue) => InvalidateProperties();

    private void OnPadOffsetChanged(Point3D oldValue, Point3D newValue) => _galleon?.UpdateCannonID(this);

    /// <summary>Ствол смотрит в сторону борта/носа, на котором стоит (см. <see cref="PadOffset" />).</summary>
    public Direction GetFacing()
    {
        var galleon = _galleon;

        if (galleon == null)
        {
            return Direction.North;
        }

        if (_padOffset.X == 0)
        {
            return galleon.Facing;
        }

        var local = _padOffset.X < 0 ? Direction.West : Direction.East;

        return (Direction)(((int)local + (int)galleon.Facing) & 0x7);
    }

    public DamageLevel GetDamageLevel() => Durability switch
    {
        >= 100.0 => DamageLevel.Pristine,
        >= 75.0  => DamageLevel.Slightly,
        >= 50.0  => DamageLevel.Moderately,
        >= 25.0  => DamageLevel.Heavily,
        _        => DamageLevel.Severely
    };

    /// <summary>Пушка получила повреждения (её задело выстрелом или взрывом).</summary>
    public virtual void OnDamage(int damage, Mobile shooter)
    {
        if (Deleted || damage <= 0)
        {
            return;
        }

        Hits -= damage;
    }

    /// <summary>Ствол уничтожен: сообщаем всем рядом и удаляем.</summary>
    protected void CheckDestroyed(Mobile from)
    {
        if (_hits > 0 || Deleted)
        {
            return;
        }

        DoAreaMessage(1116297, 5, null); // The ship cannon has been destroyed!
        from?.SendLocalizedMessage(1116297);

        Delete();
    }

    #region Цикл перезарядки

    /// <summary>Чистка банником. Без банника или для чистого ствола — отказ.</summary>
    public bool TryClean(Mobile from)
    {
        if (_cleaned && _cleanliness == 0)
        {
            from.SendLocalizedMessage(1116007); // The cannon is already clean
            return false;
        }

        if (!HasInPack(from, typeof(Swab)))
        {
            from.SendLocalizedMessage(1149659); // You need a swab.
            return false;
        }

        DoAreaMessage(1116034, 10, from); // ~1_NAME~ begins cleaning the cannon with a cannon swab.

        Schedule(ActionTime, () => Clean(from));

        return true;
    }

    private void Clean(Mobile from)
    {
        if (Deleted || from?.Deleted != false)
        {
            return;
        }

        if (!IsAtCannon(from))
        {
            DoAreaMessage(1116055, 10, from); // ~1_NAME~ cancels the effort of cleaning the cannon.
            return;
        }

        Cleaned = true;
        Cleanliness = 0;

        DoAreaMessage(1116060, 10, from); // ~1_NAME~ finishes cleaning the cannon.
    }

    /// <summary>Заряд пороха.</summary>
    public bool TryCharge(Mobile from)
    {
        if (_charged)
        {
            from.SendLocalizedMessage(1116012); // The cannon is already charged.
            return false;
        }

        if (!_cleaned)
        {
            from.SendMessage(0x22, "Ствол грязный — сначала вычистите его банником.");
            return false;
        }

        if (!HasInPack(from, typeof(PowderCharge)))
        {
            from.SendLocalizedMessage(1149665); // Need powder charge.
            return false;
        }

        DoAreaMessage(1116035, 10, from); // ~1_NAME~ begins loading the cannon with a powder charge.

        Schedule(ActionTime, () => Charge(from));

        return true;
    }

    private void Charge(Mobile from)
    {
        if (Deleted || from?.Deleted != false)
        {
            return;
        }

        if (!IsAtCannon(from))
        {
            DoAreaMessage(1116056, 10, from); // ~1_NAME~ cancels the effort of charging the cannon...
            return;
        }

        if (!ConsumeFromPack(from, typeof(PowderCharge)))
        {
            from.SendLocalizedMessage(1149665);
            return;
        }

        Charged = true;

        DoAreaMessage(1116061, 10, from); // ~1_NAME~ finishes charging the cannon.
    }

    /// <summary>Забить снаряд: шомпол в рюкзаке, затем прицел на боеприпас.</summary>
    public bool TryLoad(Mobile from)
    {
        if (!_charged)
        {
            from.SendLocalizedMessage(1116018); // ...needs to be charged with a rammer and powder charge...
            return false;
        }

        if (_ammoType != AmmunitionType.Empty)
        {
            from.SendLocalizedMessage(1149663); // Must unload first.
            return false;
        }

        if (!HasInPack(from, typeof(Ramrod)))
        {
            from.SendLocalizedMessage(1149660); // You need a ramrod.
            return false;
        }

        from.Target = new LoadTarget(this);
        from.SendLocalizedMessage(1149666); // Select Ammunition

        return true;
    }

    /// <summary>Загрузка выбранного прицелом снаряда (с задержкой, как в ServUO).</summary>
    public void DoLoad(Mobile from, Item ammo)
    {
        if (ammo is not ICannonAmmo cannonAmmo)
        {
            return;
        }

        var cliloc = cannonAmmo.AmmoType == AmmunitionType.Grapeshot ? 1116037 : 1116036;

        DoAreaMessage(cliloc, 10, from); // ~1_NAME~ begins loading the cannon with ammunition.

        Schedule(ActionTime, () => Load(from, ammo));
    }

    private void Load(Mobile from, Item ammo)
    {
        if (Deleted || from?.Deleted != false || ammo?.Deleted != false)
        {
            return;
        }

        if (_ammoType != AmmunitionType.Empty)
        {
            from.SendLocalizedMessage(1149663); // Must unload first.
            return;
        }

        if (!IsAtCannon(from))
        {
            DoAreaMessage(1116057, 10, from); // ~1_NAME~ cancels the effort of loading the cannon.
            return;
        }

        var ammoType = ((ICannonAmmo)ammo).AmmoType;

        if (!ConsumeItem(ammo))
        {
            return;
        }

        AmmoType = ammoType;

        DoAreaMessage(1116062, 10, from); // ~1_NAME~ finishes loading the cannon with a cannonball.
    }

    /// <summary>Взвести фитиль.</summary>
    public bool TryPrime(Mobile from)
    {
        if (_primed)
        {
            from.SendLocalizedMessage(1116019); // The cannon is already primed and ready to be fired.
            return false;
        }

        if (!_charged || _ammoType == AmmunitionType.Empty)
        {
            from.SendLocalizedMessage(1116021); // ...needs to be charged and loaded before it can be primed.
            return false;
        }

        if (!HasInPack(from, typeof(FuseCord)))
        {
            from.SendLocalizedMessage(1149661); // you need a fuse.
            return false;
        }

        DoAreaMessage(1116038, 10, from); // ~1_NAME~ begins priming the cannon with a cannon fuse.

        Schedule(ActionTime, () => Prime(from));

        return true;
    }

    private void Prime(Mobile from)
    {
        if (Deleted || from?.Deleted != false)
        {
            return;
        }

        if (!IsAtCannon(from))
        {
            DoAreaMessage(1116059, 10, from); // ~1_NAME~ cancels the effort of priming the cannon...
            return;
        }

        if (!ConsumeFromPack(from, typeof(FuseCord)))
        {
            from.SendLocalizedMessage(1149661);
            return;
        }

        Primed = true;

        DoAreaMessage(1116064, 10, from); // ~1_NAME~ finishes priming the cannon. It is ready to be fired!
    }

    #endregion

    #region Выстрел

    /// <summary>
    ///     Поджог фитиля: нужен горящий источник огня — спичка или факел в рюкзаке/в руке.
    ///     В ServUO ровно этот набор источников.
    /// </summary>
    public bool TryLightFuse(Mobile from)
    {
        if (!HasFireSource(from))
        {
            from.SendLocalizedMessage(1149669); // Need a lighted fire source.
            return false;
        }

        LightFuse(from);

        return true;
    }

    /// <summary>Фитиль зажжён: через <see cref="FuseDelay" /> ствол выстрелит по направлению ствола.</summary>
    public void LightFuse(Mobile from) => LightFuse(from, null, Point3D.Zero);

    /// <summary>Прицеливание по конкретной цели (корабль, моб или точка) с последующим выстрелом.</summary>
    public bool TryFireAt(Mobile from, object targeted)
    {
        if (Deleted)
        {
            return false;
        }

        if (!CanLight)
        {
            from.SendLocalizedMessage(1116021); // ...needs to be charged and loaded before it can be primed.
            return false;
        }

        if (!HasFireSource(from))
        {
            from.SendLocalizedMessage(1149669); // Need a lighted fire source.
            return false;
        }

        var aimed = ResolveTargetShip(targeted);

        if (aimed != null && ReferenceEquals(aimed, _galleon))
        {
            from.SendMessage(0x22, "Свой корабль обстреливать нельзя.");
            return false;
        }

        var point = ResolvePoint(targeted);

        if (!CannonShot.TryGetFireDirection(this, point, out _))
        {
            from.SendMessage(0x22, "Ствол не смотрит в ту сторону — разверните корабль.");
            return false;
        }

        if (!CannonShot.IsInRange(this, point))
        {
            from.SendMessage(0x22, "Цель вне дальности выстрела.");
            return false;
        }

        LightFuse(from, aimed, point);

        return true;
    }

    public void LightFuse(Mobile from, BaseGalleon aimed, Point3D point)
    {
        if (!CheckRegion(from))
        {
            return;
        }

        switch (GetDamageLevel())
        {
            case DamageLevel.Severely:
                from.SendLocalizedMessage(1116608); // The cannon is severely damaged...
                return;
            case DamageLevel.Heavily:
            case DamageLevel.Moderately:
                from.SendLocalizedMessage(1116607); // ...safe to fire.
                break;
            case DamageLevel.Slightly:
                from.SendLocalizedMessage(1116606); // ...needs some minor repair.
                break;
        }

        DoAreaMessage(1116080, 10, from); // ~1_NAME~ lights the fuse!
        from.SendLocalizedMessage(1149683); // The fuse is lit!

        Effects.PlaySound(Location, Map, 0x666);

        Schedule(FuseDelay, () => Shoot(from, aimed, point));
    }

    /// <summary>Выстрел по направлению ствола (ServUO-режим «без прицела»).</summary>
    public void Shoot(Mobile shooter) => Shoot(shooter, null, Point3D.Zero);

    /// <summary>
    ///     Выстрел. Если цель выбрана прицелом и это корабль — попадание по нему; иначе ствол
    ///     «прогрызает» линию полёта по направлению <see cref="Facing" /> и бьёт всё, что на ней.
    /// </summary>
    public void Shoot(Mobile shooter, BaseGalleon aimed, Point3D point)
    {
        if (Deleted)
        {
            return;
        }

        var ammo = AmmoInfo.GetAmmoInfo(_ammoType);

        if (ammo == null)
        {
            ClearCannon();
            return;
        }

        if (shooter?.Player == true)
        {
            Hits -= Utility.RandomMinMax(0, 4);
        }

        CannonShot.MuzzleEffects(this);
        shooter?.SendLocalizedMessage(1149691); // Fired successfully.

        if (aimed?.Deleted == false)
        {
            CannonShot.HitShip(this, aimed, point, shooter);
        }
        else
        {
            ShootAlongFacing(shooter, ammo);
        }

        ClearCannon();
    }

    /// <summary>Линия огня по направлению ствола (в ServUO — тот же обход клеток).</summary>
    private void ShootAlongFacing(Mobile shooter, AmmoInfo ammo)
    {
        var map = Map;

        if (map == null || map == Map.Internal)
        {
            return;
        }

        var stepX = 0;
        var stepY = 0;

        switch (Facing)
        {
            case Direction.North:
                stepY = -1;
                break;
            case Direction.South:
                stepY = 1;
                break;
            case Direction.West:
                stepX = -1;
                break;
            case Direction.East:
                stepX = 1;
                break;
        }

        var lateral = 1;
        var origin = Location;

        for (var range = 1; range <= Range; range++)
        {
            if (range % ammo.LateralOffset == 0)
            {
                lateral++;
            }

            for (var offset = -lateral; offset <= lateral; offset++)
            {
                var point = stepX == 0
                    ? new Point3D(origin.X + offset, origin.Y + stepY * range, origin.Z)
                    : new Point3D(origin.X + stepX * range, origin.Y + offset, origin.Z);

                if (CannonShot.TryHitBeacon(this, shooter, point, map))
                {
                    return;
                }

                var target = CannonShot.FindShip(this, point, map);

                if (target != null)
                {
                    // Залп останавливается на первой цели (в ServUO все снаряды SingleTarget).
                    CannonShot.HitShip(this, target, point, shooter);
                    return;
                }

                var mobs = CannonShot.CollectMobiles(this, shooter, point, map);

                if (mobs.Count == 0)
                {
                    continue;
                }

                CannonShot.HitMobiles(this, mobs, point, shooter);

                return;
            }
        }
    }

    /// <summary>После выстрела ствол грязный и пустой (в ServUO — <c>ClearCannon</c>).</summary>
    public void ClearCannon()
    {
        if (_cleanliness + 1 >= 10)
        {
            Cleaned = false;
        }

        Cleanliness = _cleanliness + 1;
        Charged = false;
        Primed = false;
        AmmoType = AmmunitionType.Empty;
    }

    /// <summary>Нельзя стрелять в черте города (в ServUO — <c>CheckRegion</c>).</summary>
    public static bool CheckRegion(Mobile from)
    {
        var map = from?.Map;

        // Вне мира (тестовый хост, хранение на складе) городских регионов нет.
        if (map == null || map == Map.Internal)
        {
            return from != null;
        }

        if (Region.Find(from.Location, map) is GuardedRegion { GuardsDisabled: false })
        {
            from.SendMessage(0x22, "В черте города стрелять из пушек запрещено.");
            return false;
        }

        return true;
    }

    #endregion

    #region Ремонт и разборка

    /// <summary>Ремонт ствола железом (в ServUO — <c>TryRepairCannon</c>).</summary>
    public bool TryRepairCannon(Mobile from)
    {
        if (from?.Backpack == null)
        {
            return false;
        }

        if (GetDamageLevel() == DamageLevel.Pristine)
        {
            from.SendLocalizedMessage(1116604); // The cannon is in pristine condition and does not need repairs.
            return false;
        }

        var pack = from.Backpack;
        double ingotsNeeded = 36.0 * (100 - (int)Durability);
        ingotsNeeded -= from.Skills[SkillName.Blacksmith].Value / 200.0 * ingotsNeeded;

        var min = ingotsNeeded / 10.0;
        var available = pack.GetAmount(typeof(IronIngot));

        if (available < min)
        {
            from.SendLocalizedMessage(1116603, $"{(int)min}"); // You need a minimum of ~1_METAL~ iron ingots...
            return false;
        }

        var used = Math.Min(available, ingotsNeeded);
        var repaired = available >= ingotsNeeded ? 100.0 : available / ingotsNeeded * 100.0;

        pack.ConsumeTotal(typeof(IronIngot), (int)used);

        Hits += (int)((MaxHits - _hits) * (repaired / 100.0));

        from.SendLocalizedMessage(1116605, $"{(int)used}\t{(int)Durability}"); // You make repairs...

        return true;
    }

    /// <summary>Разобрать пушку в дей (в ServUO — <c>DismantleContext</c>).</summary>
    public bool TryDismantle(Mobile from)
    {
        if (!_cleaned || _primed || _charged || _ammoType != AmmunitionType.Empty)
        {
            from.SendLocalizedMessage(1116321); // ...must be cleaned and fully unloaded before it can be dismantled.
            return false;
        }

        if (GetDamageLevel() != DamageLevel.Pristine)
        {
            from.SendLocalizedMessage(1116322); // ...must be fully repaired before it can be dismantled.
            return false;
        }

        var deed = GetDeed;

        if (deed == null)
        {
            return false;
        }

        DoAreaMessage(1116073, 10, from); // ~1_NAME~ dismantles the ship cannon.

        Delete();

        if (from.Backpack?.TryDropItem(from, deed, false) != true)
        {
            deed.MoveToWorld(from.Location, from.Map);
        }

        return true;
    }

    #endregion

    #region Вспомогательное

    /// <summary>Пушка в пределах вытянутой руки.</summary>
    protected bool IsAtCannon(Mobile from) => from?.Map == Map && from.InRange(Location, 3);

    protected static bool HasInPack(Mobile from, Type type) => from?.Backpack?.GetAmount(type) >= 1;

    protected static bool ConsumeFromPack(Mobile from, Type type) =>
        from?.Backpack?.ConsumeTotal(type, 1) == true;

    protected static bool ConsumeItem(Item item)
    {
        if (item?.Deleted != false)
        {
            return false;
        }

        item.Consume();

        return true;
    }

    /// <summary>Горящая спичка или факел в рюкзаке/в руке.</summary>
    internal static bool HasFireSource(Mobile from)
    {
        if (from == null)
        {
            return false;
        }

        var pack = from.Backpack;

        if (pack != null)
        {
            foreach (var match in pack.FindItemsByType<Matches>())
            {
                if (match.IsLit)
                {
                    return true;
                }
            }

            foreach (var torch in pack.FindItemsByType<Torch>())
            {
                if (torch.Burning)
                {
                    return true;
                }
            }
        }

        return from.FindItemOnLayer(Layer.TwoHanded) switch
        {
            Matches match => match.IsLit,
            Torch torch   => torch.Burning,
            _             => false
        };
    }

    /// <summary>Сообщение всем игрокам рядом (в ServUO — <c>DoAreaMessage</c>).</summary>
    public void DoAreaMessage(int cliloc, int range, Mobile from)
    {
        var map = Map;

        if (map == null || map == Map.Internal)
        {
            return;
        }

        foreach (var mob in map.GetMobilesInRange<PlayerMobile>(Location, range))
        {
            if (mob.Deleted || mob == from || !mob.InLOS(this))
            {
                continue;
            }

            if (from != null)
            {
                mob.SendLocalizedMessage(cliloc, from.Name);
            }
            else
            {
                mob.SendLocalizedMessage(cliloc);
            }
        }
    }

    internal static BaseGalleon ResolveTargetShip(object targeted) => targeted switch
    {
        BaseGalleon galleon => galleon,
        BaseBoat boat       => boat as BaseGalleon,
        _                   => null
    };

    internal static Point3D ResolvePoint(object targeted) => targeted switch
    {
        BaseGalleon galleon => galleon.GetMarkedLocation(),
        BaseBoat boat       => boat.GetMarkedLocation(),
        Mobile mob          => mob.Location,
        Item item           => item.GetWorldLocation(),
        IPoint3D point      => new Point3D(point),
        _                   => Point3D.Zero
    };

    /// <summary>Откладывает шаг цикла; токен отменяется вместе с пушкой.</summary>
    protected void Schedule(TimeSpan delay, Action action)
    {
        for (var i = _tokens.Count - 1; i >= 0; i--)
        {
            if (!_tokens[i].Running)
            {
                _tokens.RemoveAt(i);
            }
        }

        Timer.StartTimer(delay, action, out var token);
        _tokens.Add(token);
    }

    protected void CancelTimers()
    {
        for (var i = 0; i < _tokens.Count; i++)
        {
            _tokens[i].Cancel();
        }

        _tokens.Clear();
    }

    public override void OnLocationChange(Point3D oldLocation)
    {
        base.OnLocationChange(oldLocation);

        // Поворот корабля меняет борт и арт ствола; заодно обновляем подпись места.
        _galleon?.UpdateCannonID(this);
    }

    public override void OnDelete()
    {
        CancelTimers();

        var galleon = _galleon;
        _galleon = null;
        galleon?.RemoveCannon(this);

        base.OnDelete();
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1116025, _cleaned ? "#1116031" : "#1116032"); // Cleaned: ~1_VALUE~
        list.Add(1116026, _charged ? "#1116031" : "#1116032"); // Charged: ~1_VALUE~
        list.Add(1116027, AmmoInfo.GetAmmoName(_ammoType)); // Ammo: ~1_VALUE~
        list.Add(1116028, _primed ? "#1116031" : "#1116032"); // Primed: ~1_VALUE~
        list.Add(1116580 + (int)GetDamageLevel()); // состояние ствола
    }

    public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
    {
        base.GetContextMenuEntries(from, ref list);

        if (_galleon == null || _galleon.GetSecurityLevel(from) < SecurityLevel.Crewman)
        {
            return;
        }

        if (!_cleaned)
        {
            list.Add(new CannonActionEntry(this, from, CannonMenuAction.Clean));
        }

        if (!_charged && _cleaned)
        {
            list.Add(new CannonActionEntry(this, from, CannonMenuAction.Charge));
        }

        if (_charged && _ammoType == AmmunitionType.Empty)
        {
            list.Add(new CannonActionEntry(this, from, CannonMenuAction.Load));
        }

        if (!_primed && _charged && _ammoType != AmmunitionType.Empty)
        {
            list.Add(new CannonActionEntry(this, from, CannonMenuAction.Prime));
        }

        if (CanLight)
        {
            list.Add(new CannonActionEntry(this, from, CannonMenuAction.Fire));
        }

        if (GetDamageLevel() != DamageLevel.Pristine)
        {
            list.Add(new CannonActionEntry(this, from, CannonMenuAction.Repair));
        }

        if (_galleon.GetSecurityLevel(from) >= SecurityLevel.Officer)
        {
            list.Add(new CannonActionEntry(this, from, CannonMenuAction.Dismantle));
        }
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (from == null || Deleted)
        {
            return;
        }

        if (!from.InRange(Location, 3))
        {
            from.SendLocalizedMessage(1149687); // You are too far away.
            return;
        }

        if (_galleon == null || _galleon.GetSecurityLevel(from) < SecurityLevel.Crewman)
        {
            from.SendMessage(0x22, "Пушкой может управлять только экипаж корабля.");
            return;
        }

        switch (GetDamageLevel())
        {
            case DamageLevel.Slightly:
                from.SendLocalizedMessage(1116606);
                break;
            case DamageLevel.Moderately:
                from.SendLocalizedMessage(1116607);
                break;
            case DamageLevel.Severely:
                from.SendLocalizedMessage(1116608);
                break;
        }
    }

    private enum CannonMenuAction
    {
        Clean,
        Charge,
        Load,
        Prime,
        Fire,
        Repair,
        Dismantle
    }

    private class CannonActionEntry : ContextMenuEntry
    {
        private readonly BaseCannon _cannon;
        private readonly Mobile _from;
        private readonly CannonMenuAction _action;

        public CannonActionEntry(BaseCannon cannon, Mobile from, CannonMenuAction action) : base(GetLabel(action), 3)
        {
            _cannon = cannon;
            _from = from;
            _action = action;
        }

        private static int GetLabel(CannonMenuAction action) => action switch
        {
            CannonMenuAction.Clean  => 1149626,
            CannonMenuAction.Charge => 1149630,
            CannonMenuAction.Load   => 1149635,
            CannonMenuAction.Prime  => 1149637,
            CannonMenuAction.Fire   => 1149638,
            CannonMenuAction.Repair => 1116602,
            _                       => 1116069
        };

        public override void OnClick(Mobile from, IEntity target)
        {
            if (_cannon.Deleted || _cannon.Galleon == null)
            {
                return;
            }

            switch (_action)
            {
                case CannonMenuAction.Clean:
                    _cannon.TryClean(_from);
                    break;
                case CannonMenuAction.Charge:
                    _cannon.TryCharge(_from);
                    break;
                case CannonMenuAction.Load:
                    _cannon.TryLoad(_from);
                    break;
                case CannonMenuAction.Prime:
                    _cannon.TryPrime(_from);
                    break;
                case CannonMenuAction.Fire:
                    _from.Target = new FireTarget(_cannon);
                    break;
                case CannonMenuAction.Repair:
                    _cannon.TryRepairCannon(_from);
                    break;
                case CannonMenuAction.Dismantle:
                    _cannon.TryDismantle(_from);
                    break;
            }
        }
    }

    /// <summary>Прицел на боеприпас при зарядке.</summary>
    private class LoadTarget : Target
    {
        private readonly BaseCannon _cannon;

        public LoadTarget(BaseCannon cannon) : base(3, false, TargetFlags.None) => _cannon = cannon;

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (_cannon.Deleted)
            {
                return;
            }

            if (targeted is not Item item || item is not ICannonAmmo)
            {
                from.SendMessage(0x22, "Это не боеприпас для пушки.");
                return;
            }

            if (item.RootParent != from)
            {
                from.SendMessage(0x22, "Боеприпас должен быть у вас в рюкзаке.");
                return;
            }

            _cannon.DoLoad(from, item);
        }
    }

    /// <summary>Прицел на цель выстрела.</summary>
    private class FireTarget : Target
    {
        private readonly BaseCannon _cannon;

        public FireTarget(BaseCannon cannon) : base(cannon.Range, false, TargetFlags.None) => _cannon = cannon;

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (!_cannon.Deleted)
            {
                _cannon.TryFireAt(from, targeted);
            }
        }
    }

    #endregion
}

[SerializationGenerator(0, false)]
public partial class LightShipCannon : BaseCannon
{
    [Constructible]
    public LightShipCannon(BaseGalleon galleon) : base(galleon)
    {
    }

    public override int Range => 8;

    public override ShipCannonDeed GetDeed => new LightShipCannonDeed();
}

[SerializationGenerator(0, false)]
public partial class HeavyShipCannon : BaseCannon
{
    [Constructible]
    public HeavyShipCannon(BaseGalleon galleon) : base(galleon)
    {
    }

    public override int Range => 10;

    public override TimeSpan ActionTime => TimeSpan.FromSeconds(2.0);

    public override CannonPower Power => CannonPower.Heavy;

    public override int LabelNumber => 0;

    public override ShipCannonDeed GetDeed => new HeavyShipCannonDeed();
}
